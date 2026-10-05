using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentLedger.Infrastructure.Persistence;

internal sealed class DesignTimeLedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    private const string MigrationsConnectionString =
        "Host=localhost;Database=payment_ledger;Username=postgres;Password=postgres";

    public LedgerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseNpgsql(MigrationsConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new LedgerDbContext(options);
    }
}
