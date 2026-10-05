using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Accounts;

public interface IAccountRepository
{
    Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> ListByIdsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken);

    // Rows are locked in ascending id order so two requests touching the same accounts can never deadlock.
    Task<LockedAccounts> LockAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken);

    void Add(Account account);
}
