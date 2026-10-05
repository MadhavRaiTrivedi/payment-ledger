using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Idempotency;

internal sealed class IdempotencyStore(LedgerDbContext db) : IIdempotencyStore
{
    public async Task<bool> TryClaimAsync(IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        // An expired record is taken over in place, so keys stay usable even if the cleanup job is behind.
        var claimedRows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO idempotency_records (owner_id, idempotency_key, request_hash, created_at, expires_at)
            VALUES ({claim.OwnerId}, {claim.Key}, {claim.RequestHash}, {claim.CreatedAt}, {claim.ExpiresAt})
            ON CONFLICT (owner_id, idempotency_key) DO UPDATE
            SET request_hash = EXCLUDED.request_hash,
                response_body = NULL,
                created_at = EXCLUDED.created_at,
                expires_at = EXCLUDED.expires_at
            WHERE idempotency_records.expires_at <= EXCLUDED.created_at
            """,
            cancellationToken);

        return claimedRows == 1;
    }

    public async Task<IdempotencyRecord?> FindAsync(Guid ownerId, string key, CancellationToken cancellationToken) =>
        await db.IdempotencyRecords
            .AsNoTracking()
            .Where(record => record.OwnerId == ownerId && record.IdempotencyKey == key)
            .Select(record => new IdempotencyRecord(record.RequestHash, record.ResponseBody))
            .SingleOrDefaultAsync(cancellationToken);

    public Task CompleteAsync(Guid ownerId, string key, string responseBody, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE idempotency_records
            SET response_body = CAST({responseBody} AS jsonb)
            WHERE owner_id = {ownerId} AND idempotency_key = {key}
            """,
            cancellationToken);

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        db.IdempotencyRecords.Where(record => record.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
}
