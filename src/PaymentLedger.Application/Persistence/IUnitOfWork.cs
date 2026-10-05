namespace PaymentLedger.Application.Persistence;

public interface IUnitOfWork
{
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
