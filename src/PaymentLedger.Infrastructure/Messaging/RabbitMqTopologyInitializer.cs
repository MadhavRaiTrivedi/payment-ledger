using RabbitMQ.Client;

namespace PaymentLedger.Infrastructure.Messaging;

public static class RabbitMqTopologyInitializer
{
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            MessagingTopology.LedgerEventsExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            MessagingTopology.DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            MessagingTopology.NotificationsDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            MessagingTopology.NotificationsDeadLetterQueue,
            MessagingTopology.DeadLetterExchange,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            MessagingTopology.NotificationsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = MessagingTopology.DeadLetterExchange },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            MessagingTopology.NotificationsQueue,
            MessagingTopology.LedgerEventsExchange,
            MessagingTopology.AllTransactionEvents,
            cancellationToken: cancellationToken);
    }
}
