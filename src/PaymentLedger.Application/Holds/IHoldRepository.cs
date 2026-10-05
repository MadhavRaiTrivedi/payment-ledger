using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Application.Holds;

public interface IHoldRepository
{
    Task<Hold?> FindAsync(Guid holdId, CancellationToken cancellationToken);

    Task<Hold?> LockAsync(Guid holdId, CancellationToken cancellationToken);

    // Skips holds already locked by another request, so several workers can expire holds side by side.
    Task<IReadOnlyList<Hold>> LockExpiredAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

    void Add(Hold hold);
}
