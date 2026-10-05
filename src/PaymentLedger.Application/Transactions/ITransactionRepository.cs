using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public interface ITransactionRepository
{
    Task<Transaction?> FindAsync(Guid transactionId, CancellationToken cancellationToken);

    Task<Transaction?> LockAsync(Guid transactionId, CancellationToken cancellationToken);

    void Add(Transaction transaction);
}
