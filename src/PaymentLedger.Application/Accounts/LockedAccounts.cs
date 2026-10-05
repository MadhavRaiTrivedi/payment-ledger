using PaymentLedger.Application.Errors;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Accounts;

public sealed class LockedAccounts(IReadOnlyCollection<Account> accounts)
{
    private readonly Dictionary<Guid, Account> _accountsById = accounts.ToDictionary(account => account.Id);

    public IReadOnlyCollection<Account> All => accounts;

    public Account Get(Guid accountId) =>
        _accountsById.TryGetValue(accountId, out var account)
            ? account
            : throw new NotFoundException(nameof(Account), accountId);
}
