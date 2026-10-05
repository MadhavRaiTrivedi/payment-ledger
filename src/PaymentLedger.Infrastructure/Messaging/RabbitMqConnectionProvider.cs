using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PaymentLedger.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.Value.ConnectionString),
                ClientProvidedName = options.Value.ClientName,
                AutomaticRecoveryEnabled = true,
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectLock.Dispose();
    }
}
