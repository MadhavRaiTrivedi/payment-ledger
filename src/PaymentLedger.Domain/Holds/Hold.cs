using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Domain.Holds;

public sealed class Hold
{
    private static readonly Dictionary<HoldStatus, HoldStatus[]> AllowedTransitions = new()
    {
        [HoldStatus.Active] = [HoldStatus.Captured, HoldStatus.Released, HoldStatus.Expired],
        [HoldStatus.Captured] = [],
        [HoldStatus.Released] = [],
        [HoldStatus.Expired] = [],
    };

    private Hold()
    {
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid BeneficiaryAccountId { get; private set; }

    public long AmountInPaise { get; private set; }

    public Currency Currency { get; private set; }

    public HoldStatus Status { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public Guid? CaptureTransactionId { get; private set; }

    public Money Amount => new(AmountInPaise, Currency);

    public bool IsExpiredAt(DateTimeOffset moment) => moment >= ExpiresAt;

    public static Hold Place(Account account, Account beneficiary, Money amount, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        amount.EnsurePositive();
        if (!account.IsCustomerWallet || !beneficiary.IsCustomerWallet || account.Id == beneficiary.Id)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidCounterparty,
                "A hold needs two different customer wallets.");
        }

        if (expiresAt <= now)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidHoldExpiry,
                $"Hold expiry {expiresAt:O} must be in the future.");
        }

        beneficiary.EnsureActive();
        account.ReserveFunds(amount);

        return new Hold
        {
            Id = Guid.CreateVersion7(now),
            AccountId = account.Id,
            BeneficiaryAccountId = beneficiary.Id,
            AmountInPaise = amount.AmountInPaise,
            Currency = amount.Currency,
            Status = HoldStatus.Active,
            ExpiresAt = expiresAt,
            CreatedAt = now,
        };
    }

    public void EnsureCapturable(Money amount, DateTimeOffset now)
    {
        EnsureActive();
        amount.EnsurePositive();
        if (IsExpiredAt(now))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.HoldExpired,
                $"Hold {Id} expired at {ExpiresAt:O}.");
        }

        if (amount > Amount)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.CaptureExceedsHold,
                $"Cannot capture {amount} from a hold of {Amount}.");
        }
    }

    public void Release(Account account, DateTimeOffset now) => Finish(HoldStatus.Released, account, now);

    public void Expire(Account account, DateTimeOffset now)
    {
        if (!IsExpiredAt(now))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.HoldNotYetExpired,
                $"Hold {Id} expires at {ExpiresAt:O}.");
        }

        Finish(HoldStatus.Expired, account, now);
    }

    // A partial capture still releases the whole hold, so the uncaptured part becomes available again.
    internal void MarkCaptured(Guid transactionId, Account account, DateTimeOffset now)
    {
        Finish(HoldStatus.Captured, account, now);
        CaptureTransactionId = transactionId;
    }

    private void Finish(HoldStatus newStatus, Account account, DateTimeOffset now)
    {
        if (account.Id != AccountId)
        {
            throw new ArgumentException($"Hold {Id} belongs to account {AccountId}, not {account.Id}.", nameof(account));
        }

        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidStatusTransition,
                $"Hold {Id} cannot move from {Status} to {newStatus}.");
        }

        account.ReleaseReservedFunds(Amount);
        Status = newStatus;
        CompletedAt = now;
    }

    private void EnsureActive()
    {
        if (Status != HoldStatus.Active)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.HoldNotActive,
                $"Hold {Id} is {Status}.");
        }
    }
}
