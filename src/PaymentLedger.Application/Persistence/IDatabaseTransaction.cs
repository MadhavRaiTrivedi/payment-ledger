namespace PaymentLedger.Application.Persistence;

public interface IDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
