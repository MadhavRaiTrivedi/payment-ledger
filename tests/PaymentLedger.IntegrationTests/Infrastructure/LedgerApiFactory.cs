using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PaymentLedger.IntegrationTests.Infrastructure;

public sealed class LedgerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const long TransferFeeInPaise = 500;

    public async ValueTask InitializeAsync()
    {
        await TestContainers.EnsureStartedAsync();
        using var warmUp = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Ledger", TestContainers.Postgres.GetConnectionString());
        builder.UseSetting("RabbitMq:ConnectionString", TestContainers.RabbitMq.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("Jwt:EnableDevTokenEndpoint", "true");
        builder.UseSetting("Ledger:TransferFeeInPaise", TransferFeeInPaise.ToString());
    }
}
