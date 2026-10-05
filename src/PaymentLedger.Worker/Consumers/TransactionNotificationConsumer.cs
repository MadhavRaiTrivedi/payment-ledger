using System.Text.Json;
using PaymentLedger.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentLedger.Worker.Consumers;

// Stands in for a downstream service such as customer notifications. It shows the consumer side of
// at-least-once delivery: duplicates are skipped, and a message that keeps failing goes to the dead-letter queue.
internal sealed class TransactionNotificationConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionNotificationConsumer> logger) : BackgroundService
{
    private const string ConsumerName = "transaction-notifications";
    private const ushort PrefetchCount = 20;

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await RabbitMqTopologyInitializer.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(_channel, delivery, stoppingToken);
        await _channel.BasicConsumeAsync(MessagingTopology.NotificationsQueue, autoAck: false, consumer, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var messageId))
            {
                throw new InvalidOperationException("Message has no valid MessageId.");
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var processedMessages = scope.ServiceProvider.GetRequiredService<ProcessedMessageStore>();
            if (await processedMessages.TryMarkProcessedAsync(ConsumerName, messageId, cancellationToken))
            {
                Notify(delivery);
            }
            else
            {
                logger.LogInformation("Skipping duplicate message {MessageId}", messageId);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Message {DeliveryTag} could not be processed and was dead-lettered", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    private void Notify(BasicDeliverEventArgs delivery)
    {
        using var payload = JsonDocument.Parse(delivery.Body);
        var transactionId = payload.RootElement.GetProperty("transactionId").GetGuid();
        logger.LogInformation(
            "Notifying customers about {EventType} for transaction {TransactionId}",
            delivery.BasicProperties.Type,
            transactionId);
    }
}
