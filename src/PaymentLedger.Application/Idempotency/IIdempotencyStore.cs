namespace PaymentLedger.Application.Idempotency;

public interface IIdempotencyStore
{
    // Returns false when an unexpired record already exists for this owner and key. A concurrent claim for the
    // same key waits on the unique index until the first transaction commits or rolls back.
    Task<bool> TryClaimAsync(IdempotencyClaim claim, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> FindAsync(Guid ownerId, string key, CancellationToken cancellationToken);

    Task CompleteAsync(Guid ownerId, string key, string responseBody, CancellationToken cancellationToken);

    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
