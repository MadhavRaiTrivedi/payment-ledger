using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.Infrastructure.Messaging;
using PaymentLedger.Infrastructure.Outbox;
using PaymentLedger.IntegrationTests.Infrastructure;
using RabbitMQ.Client;

namespace PaymentLedger.IntegrationTests.Messaging;

public class OutboxPublishingTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>
{
    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PostedTransaction_IsPublishedToRabbitMqWithItsOutboxId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await new ConnectionFactory
        {
            Uri = new Uri(TestContainers.RabbitMq.GetConnectionString()),
        }.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMqTopologyInitializer.DeclareAsync(channel, cancellationToken);
        var queue = (await channel.QueueDeclareAsync(cancellationToken: cancellationToken)).QueueName;
        await channel.QueueBindAsync(queue, MessagingTopology.LedgerEventsExchange, "transaction.posted", cancellationToken: cancellationToken);

        var admin = await LedgerClient.AdminAsync(factory);
        var wallet = await admin.OpenWalletAsync(Guid.NewGuid());
        var deposit = await admin.FundAsync(wallet.Id, 7_000);

        await DrainOutboxAsync(cancellationToken);

        var message = await WaitForMessageAboutAsync(channel, queue, deposit.Id, cancellationToken);
        message.BasicProperties.Type.ShouldBe(nameof(TransactionPosted));
        Guid.TryParse(message.BasicProperties.MessageId, out _).ShouldBeTrue();
    }

    private async Task DrainOutboxAsync(CancellationToken cancellationToken)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
        while (await publisher.PublishPendingAsync(cancellationToken) > 0)
        {
        }
    }

    private static async Task<BasicGetResult> WaitForMessageAboutAsync(
        IChannel channel,
        string queue,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + DeliveryTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var message = await channel.BasicGetAsync(queue, autoAck: true, cancellationToken);
            if (message is null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                continue;
            }

            using var payload = JsonDocument.Parse(Encoding.UTF8.GetString(message.Body.Span));
            if (payload.RootElement.GetProperty("transactionId").GetGuid() == transactionId)
            {
                return message;
            }
        }

        throw new TimeoutException($"No message about transaction {transactionId} arrived within {DeliveryTimeout}.");
    }
}
