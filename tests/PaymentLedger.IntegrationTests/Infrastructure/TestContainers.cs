using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace PaymentLedger.IntegrationTests.Infrastructure;

// Started once per test run and shared by every test class; each test works on its own wallets.
internal static class TestContainers
{
    private static readonly Lazy<Task> Startup = new(StartAsync);

    public static PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public static RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder("rabbitmq:4.1-management-alpine").Build();

    public static Task EnsureStartedAsync() => Startup.Value;

    private static Task StartAsync() => Task.WhenAll(Postgres.StartAsync(), RabbitMq.StartAsync());
}
