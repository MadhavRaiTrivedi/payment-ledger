using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Domain.Transactions;

public sealed class Transaction : AggregateRoot
{
    private const int MinimumEntryCount = 2;

    private static readonly Dictionary<TransactionStatus, TransactionStatus[]> AllowedTransitions = new()
    {
        [TransactionStatus.Pending] = [TransactionStatus.Posted, TransactionStatus.Failed],
        [TransactionStatus.Posted] = [TransactionStatus.Reversed],
        [TransactionStatus.Failed] = [],
        [TransactionStatus.Reversed] = [],
    };

    private readonly List<Entry> _entries = [];

    private Transaction()
    {
    }

    private Transaction(
        TransactionType type,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        Money fee,
        TransactionOrigin origin,
        IEnumerable<Entry> entries,
        Guid? originalTransactionId = null)
    {
        Id = Guid.CreateVersion7(origin.RequestedAt);
        Type = type;
        Status = TransactionStatus.Pending;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Currency = amount.Currency;
        AmountInPaise = amount.AmountInPaise;
        FeeInPaise = fee.AmountInPaise;
        OriginalTransactionId = originalTransactionId;
        InitiatedBy = origin.InitiatedBy;
        CorrelationId = origin.CorrelationId;
        IdempotencyKey = origin.IdempotencyKey;
        CreatedAt = origin.RequestedAt;
        _entries.AddRange(entries);

        EnsureBalanced();
    }

    public Guid Id { get; private set; }

    public TransactionType Type { get; private set; }

    public TransactionStatus Status { get; private set; }

    public TransactionFailureReason? FailureReason { get; private set; }

    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public Currency Currency { get; private set; }

    public long AmountInPaise { get; private set; }

    public long FeeInPaise { get; private set; }

    public long RefundedInPaise { get; private set; }

    public Guid? OriginalTransactionId { get; private set; }

    public Guid? ReversalTransactionId { get; private set; }

    public Guid InitiatedBy { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyList<Entry> Entries => _entries;

    public Money Amount => new(AmountInPaise, Currency);

    public Money Fee => new(FeeInPaise, Currency);

    public Money Refunded => new(RefundedInPaise, Currency);

    public Money RefundableAmount => Amount - Refunded;

    public IReadOnlySet<Guid> AccountIds => _entries.Select(entry => entry.AccountId).ToHashSet();

    public static Transaction Deposit(Account wallet, Money amount, TransactionOrigin origin)
    {
        EnsureCustomerWallet(wallet);
        return Movement(TransactionType.Deposit, SystemAccounts.SettlementId, wallet.Id, amount, origin);
    }

    public static Transaction Withdrawal(Account wallet, Money amount, TransactionOrigin origin)
    {
        EnsureCustomerWallet(wallet);
        return Movement(TransactionType.Withdrawal, wallet.Id, SystemAccounts.SettlementId, amount, origin);
    }

    public static Transaction Transfer(Account sender, Account recipient, Money amount, Money fee, TransactionOrigin origin)
    {
        EnsureCustomerWallet(sender);
        EnsureCustomerWallet(recipient);
        if (sender.Id == recipient.Id)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidCounterparty,
                "Sender and recipient must be different accounts.");
        }

        amount.EnsurePositive();
        List<Entry> entries =
        [
            new(sender.Id, EntryDirection.Debit, amount + fee, origin.RequestedAt),
            new(recipient.Id, EntryDirection.Credit, amount, origin.RequestedAt),
        ];
        if (fee.IsPositive)
        {
            entries.Add(new Entry(SystemAccounts.FeeRevenueId, EntryDirection.Credit, fee, origin.RequestedAt));
        }

        return new Transaction(TransactionType.Transfer, sender.Id, recipient.Id, amount, fee, origin, entries);
    }

    public static Transaction HoldCapture(Hold hold, Money amount, TransactionOrigin origin)
    {
        hold.EnsureCapturable(amount, origin.RequestedAt);
        return Movement(TransactionType.HoldCapture, hold.AccountId, hold.BeneficiaryAccountId, amount, origin);
    }

    public static Transaction Refund(Transaction original, Money amount, TransactionOrigin origin)
    {
        original.EnsureCanBeRefunded(amount);
        return Movement(
            TransactionType.Refund,
            original.DestinationAccountId,
            original.SourceAccountId,
            amount,
            origin,
            original.Id);
    }

    public static Transaction Reversal(Transaction original, TransactionOrigin origin)
    {
        original.EnsureCanBeReversed();
        var mirroredEntries = original.Entries.Select(entry => entry.Mirror(origin.RequestedAt));
        return new Transaction(
            TransactionType.Reversal,
            original.DestinationAccountId,
            original.SourceAccountId,
            original.Amount,
            original.Fee,
            origin,
            mirroredEntries,
            original.Id);
    }

    public void RecordRefund(Money amount)
    {
        EnsureCanBeRefunded(amount);
        RefundedInPaise = (Refunded + amount).AmountInPaise;
    }

    public void MarkReversed(Guid reversalTransactionId, DateTimeOffset reversedAt)
    {
        EnsureCanBeReversed();
        TransitionTo(TransactionStatus.Reversed);
        ReversalTransactionId = reversalTransactionId;
        Raise(new TransactionReversed(Id, reversalTransactionId, reversedAt));
    }

    internal void MarkPosted(DateTimeOffset postedAt)
    {
        TransitionTo(TransactionStatus.Posted);
        CompletedAt = postedAt;
        Raise(new TransactionPosted(
            Id, Type, SourceAccountId, DestinationAccountId, AmountInPaise, FeeInPaise, Currency, postedAt));
    }

    internal void MarkFailed(TransactionFailureReason reason, DateTimeOffset failedAt)
    {
        TransitionTo(TransactionStatus.Failed);
        FailureReason = reason;
        CompletedAt = failedAt;
        Raise(new TransactionFailed(Id, Type, reason, failedAt));
    }

    private static Transaction Movement(
        TransactionType type,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        TransactionOrigin origin,
        Guid? originalTransactionId = null)
    {
        amount.EnsurePositive();
        Entry[] entries =
        [
            new(sourceAccountId, EntryDirection.Debit, amount, origin.RequestedAt),
            new(destinationAccountId, EntryDirection.Credit, amount, origin.RequestedAt),
        ];
        return new Transaction(
            type, sourceAccountId, destinationAccountId, amount, Money.Zero(amount.Currency), origin, entries, originalTransactionId);
    }

    private static void EnsureCustomerWallet(Account account)
    {
        if (!account.IsCustomerWallet)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidCounterparty,
                $"Account {account.Id} is a {account.Type} account, not a customer wallet.");
        }
    }

    private void EnsureCanBeRefunded(Money amount)
    {
        amount.EnsurePositive();
        if (Type != TransactionType.Transfer || Status != TransactionStatus.Posted)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.TransactionNotRefundable,
                $"Only posted transfers can be refunded. Transaction {Id} is a {Status} {Type}.");
        }

        if (amount > RefundableAmount)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.RefundExceedsOriginal,
                $"Cannot refund {amount}; only {RefundableAmount} of transaction {Id} is left to refund.");
        }
    }

    private void EnsureCanBeReversed()
    {
        if (Status != TransactionStatus.Posted)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.TransactionNotReversible,
                $"Only posted transactions can be reversed. Transaction {Id} is {Status}.");
        }

        if (Type is TransactionType.Refund or TransactionType.Reversal)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.TransactionNotReversible,
                $"A {Type} cannot be reversed.");
        }

        // Reversing a partly refunded transfer would hand the refunded money back a second time.
        if (RefundedInPaise > 0)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.TransactionNotReversible,
                $"Transaction {Id} has {Refunded} refunded and cannot be reversed.");
        }
    }

    private void EnsureBalanced()
    {
        if (_entries.Count < MinimumEntryCount)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.UnbalancedTransaction,
                $"A transaction needs at least {MinimumEntryCount} entries.");
        }

        foreach (var entry in _entries)
        {
            entry.Amount.EnsurePositive();
            if (entry.Currency != Currency)
            {
                throw new DomainRuleViolationException(
                    DomainErrorCode.CurrencyMismatch,
                    $"Entry currency {entry.Currency} does not match transaction currency {Currency}.");
            }
        }

        var debits = SumOf(EntryDirection.Debit);
        var credits = SumOf(EntryDirection.Credit);
        if (debits != credits)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.UnbalancedTransaction,
                $"Debits ({debits}) and credits ({credits}) must be equal.");
        }
    }

    private Money SumOf(EntryDirection direction) =>
        _entries
            .Where(entry => entry.Direction == direction)
            .Aggregate(Money.Zero(Currency), (total, entry) => total + entry.Amount);

    private void TransitionTo(TransactionStatus newStatus)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidStatusTransition,
                $"Transaction {Id} cannot move from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}
