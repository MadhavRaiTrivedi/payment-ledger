using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Accounts;

internal sealed class AccountRepository(LedgerDbContext db) : IAccountRepository
{
    public async Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Accounts.FindAsync([accountId], cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Accounts
            .Where(account => account.OwnerId == ownerId)
            .OrderBy(account => account.OpenedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken) =>
        await db.Accounts.Where(account => accountIds.Contains(account.Id)).ToListAsync(cancellationToken);

    public async Task<LockedAccounts> LockAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        db.EnsureInTransaction();
        var orderedIds = accountIds.Distinct().Order().ToArray();

        var locked = await db.Accounts
            .FromSql($"SELECT * FROM accounts WHERE id = ANY({orderedIds}) ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

        return new LockedAccounts(locked);
    }

    public void Add(Account account) => db.Accounts.Add(account);
}
