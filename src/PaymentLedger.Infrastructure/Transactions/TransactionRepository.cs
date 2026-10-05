using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Transactions;

internal sealed class TransactionRepository(LedgerDbContext db) : ITransactionRepository
{
    public Task<Transaction?> FindAsync(Guid transactionId, CancellationToken cancellationToken) =>
        db.Transactions
            .Include(transaction => transaction.Entries)
            .SingleOrDefaultAsync(transaction => transaction.Id == transactionId, cancellationToken);

    public async Task<Transaction?> LockAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        db.EnsureInTransaction();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM transactions WHERE id = {transactionId} FOR UPDATE", cancellationToken);

        return await FindAsync(transactionId, cancellationToken);
    }

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);
}
