using System.Globalization;

namespace PaymentLedger.Domain.Currencies;

public readonly record struct Money(long AmountInPaise, Currency Currency)
{
    private const decimal PaisePerRupee = 100m;

    public static Money Zero(Currency currency) => new(0, currency);

    public bool IsPositive => AmountInPaise > 0;

    public bool IsZero => AmountInPaise == 0;

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.AmountInPaise + right.AmountInPaise), left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.AmountInPaise - right.AmountInPaise), left.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.AmountInPaise > right.AmountInPaise;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.AmountInPaise < right.AmountInPaise;
    }

    public static bool operator >=(Money left, Money right) => !(left < right);

    public static bool operator <=(Money left, Money right) => !(left > right);

    public void EnsurePositive()
    {
        if (!IsPositive)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidAmount,
                $"Amount must be greater than zero, got {this}.");
        }
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{AmountInPaise / PaisePerRupee:0.00} {Currency}");

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.CurrencyMismatch,
                $"Cannot combine {left.Currency} and {right.Currency}.");
        }
    }
}
