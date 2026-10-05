using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Domain.Transactions;

public sealed class Entry
{
    private Entry()
    {
    }

    internal Entry(Guid accountId, EntryDirection direction, Money amount, DateTimeOffset createdAt)
    {
        AccountId = accountId;
        Direction = direction;
        AmountInPaise = amount.AmountInPaise;
        Currency = amount.Currency;
        CreatedAt = createdAt;
    }

    public long Sequence { get; private set; }

    public Guid TransactionId { get; private set; }

    public Guid AccountId { get; private set; }

    public EntryDirection Direction { get; private set; }

    public long AmountInPaise { get; private set; }

    public Currency Currency { get; private set; }

    public long? BalanceAfterInPaise { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Money Amount => new(AmountInPaise, Currency);

    internal Entry Mirror(DateTimeOffset createdAt)
    {
        var opposite = Direction == EntryDirection.Debit ? EntryDirection.Credit : EntryDirection.Debit;
        return new Entry(AccountId, opposite, Amount, createdAt);
    }

    internal void RecordBalanceAfter(Money balance) => BalanceAfterInPaise = balance.AmountInPaise;
}
