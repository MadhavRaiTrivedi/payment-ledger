using System.Text;
using PaymentLedger.Infrastructure.Outbox;
using RabbitMQ.Client;

namespace PaymentLedger.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(RabbitMqConnectionProvider connections) : IAsyncDisposable
{
    private const string JsonContentType = "application/json";

    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IChannel? _channel;

    // With publisher confirms enabled, BasicPublishAsync only returns once the broker has taken
    // responsibility for the message, and throws if the broker rejects it.
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);
        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = JsonContentType,
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
        };

        await channel.BasicPublishAsync(
            MessagingTopology.LedgerEventsExchange,
            MessagingTopology.RoutingKeyFor(message.Type),
            mandatory: false,
            properties,
            Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _channelLock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            var connection = await connections.GetConnectionAsync(cancellationToken);
            _channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken);
            await RabbitMqTopologyInitializer.DeclareAsync(_channel, cancellationToken);
            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }
}
