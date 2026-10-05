using Microsoft.EntityFrameworkCore;

namespace PaymentLedger.Infrastructure.Persistence;

internal static class DbContextLockingExtensions
{
    // Outside a transaction, FOR UPDATE releases the lock as soon as the statement ends, which would
    // silently remove the protection the caller is relying on.
    public static void EnsureInTransaction(this LedgerDbContext db)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Rows can only be locked inside a database transaction.");
        }
    }
}
