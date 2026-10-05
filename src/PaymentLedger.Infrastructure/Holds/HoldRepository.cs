using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Holds;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Holds;

internal sealed class HoldRepository(LedgerDbContext db) : IHoldRepository
{
    public async Task<Hold?> FindAsync(Guid holdId, CancellationToken cancellationToken) =>
        await db.Holds.FindAsync([holdId], cancellationToken);

    public Task<Hold?> LockAsync(Guid holdId, CancellationToken cancellationToken)
    {
        db.EnsureInTransaction();
        return db.Holds
            .FromSql($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Hold>> LockExpiredAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        db.EnsureInTransaction();
        var activeStatus = HoldStatus.Active.ToString();

        return await db.Holds
            .FromSql(
                $"""
                SELECT * FROM holds
                WHERE status = {activeStatus} AND expires_at <= {now}
                ORDER BY expires_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }

    public void Add(Hold hold) => db.Holds.Add(hold);
}
