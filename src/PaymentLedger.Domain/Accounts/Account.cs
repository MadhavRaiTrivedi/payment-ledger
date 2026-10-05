using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Domain.Accounts;

public sealed class Account
{
    private static readonly Dictionary<AccountStatus, AccountStatus[]> AllowedTransitions = new()
    {
        [AccountStatus.Active] = [AccountStatus.Frozen, AccountStatus.Closed],
        [AccountStatus.Frozen] = [AccountStatus.Active, AccountStatus.Closed],
        [AccountStatus.Closed] = [],
    };

    private Account()
    {
    }

    public Guid Id { get; private set; }

    public AccountType Type { get; private set; }

    public AccountStatus Status { get; private set; }

    public Guid? OwnerId { get; private set; }

    public Currency Currency { get; private set; }

    public long BalanceInPaise { get; private set; }

    public long HeldInPaise { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public Money Balance => new(BalanceInPaise, Currency);

    public Money Held => new(HeldInPaise, Currency);

    public Money AvailableBalance => Balance - Held;

    public bool IsActive => Status == AccountStatus.Active;

    public bool IsCustomerWallet => Type == AccountType.CustomerWallet;

    // System accounts mirror money outside customer wallets, so Settlement goes negative on every deposit.
    public bool CanGoNegative => !IsCustomerWallet;

    public static Account OpenWallet(Guid ownerId, Currency currency, DateTimeOffset openedAt) => new()
    {
        Id = Guid.CreateVersion7(openedAt),
        Type = AccountType.CustomerWallet,
        Status = AccountStatus.Active,
        OwnerId = ownerId,
        Currency = currency,
        OpenedAt = openedAt,
    };

    public static Account CreateSystemAccount(Guid id, AccountType type, Currency currency, DateTimeOffset openedAt)
    {
        if (type == AccountType.CustomerWallet)
        {
            throw new ArgumentException("A system account cannot be a customer wallet.", nameof(type));
        }

        return new Account
        {
            Id = id,
            Type = type,
            Status = AccountStatus.Active,
            Currency = currency,
            OpenedAt = openedAt,
        };
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    public void Freeze() => ChangeStatus(AccountStatus.Frozen);

    public void Unfreeze() => ChangeStatus(AccountStatus.Active);

    public void Close()
    {
        if (BalanceInPaise != 0 || HeldInPaise != 0)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AccountNotEmpty,
                $"Account {Id} still has a balance of {Balance} and {Held} on hold.");
        }

        ChangeStatus(AccountStatus.Closed);
    }

    internal Money Apply(EntryDirection direction, Money amount)
    {
        var signedAmount = direction == EntryDirection.Credit ? amount : Money.Zero(Currency) - amount;
        BalanceInPaise = (Balance + signedAmount).AmountInPaise;
        return Balance;
    }

    internal void ReserveFunds(Money amount)
    {
        EnsureActive();
        if (amount > AvailableBalance)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InsufficientFunds,
                $"Account {Id} has {AvailableBalance} available, cannot hold {amount}.");
        }

        HeldInPaise = (Held + amount).AmountInPaise;
    }

    internal void ReleaseReservedFunds(Money amount)
    {
        HeldInPaise = (Held - amount).AmountInPaise;
    }

    internal void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AccountNotActive,
                $"Account {Id} is {Status}.");
        }
    }

    private void ChangeStatus(AccountStatus newStatus)
    {
        if (!IsCustomerWallet)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.SystemAccountStatusChange,
                $"System account {Type} cannot be {newStatus.ToString().ToLowerInvariant()}.");
        }

        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidStatusTransition,
                $"Account {Id} cannot move from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}
