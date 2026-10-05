using Microsoft.EntityFrameworkCore.Storage;
using PaymentLedger.Application.Persistence;

namespace PaymentLedger.Infrastructure.Persistence;

internal sealed class UnitOfWork(LedgerDbContext db) : IUnitOfWork
{
    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfDatabaseTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private sealed class EfDatabaseTransaction(IDbContextTransaction transaction) : IDatabaseTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
