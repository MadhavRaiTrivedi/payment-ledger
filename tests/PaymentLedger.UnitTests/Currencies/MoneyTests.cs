using PaymentLedger.Domain;
using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.UnitTests.Currencies;

public class MoneyTests
{
    [Fact]
    public void Add_WithSameCurrency_SumsPaise()
    {
        var total = new Money(150, Currency.INR) + new Money(250, Currency.INR);

        total.ShouldBe(new Money(400, Currency.INR));
    }

    [Fact]
    public void Subtract_BelowZero_ReturnsNegativeAmount()
    {
        var difference = new Money(100, Currency.INR) - new Money(250, Currency.INR);

        difference.AmountInPaise.ShouldBe(-150);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnsurePositive_WhenNotPositive_Throws(long amountInPaise)
    {
        var amount = new Money(amountInPaise, Currency.INR);

        var exception = Should.Throw<DomainRuleViolationException>(amount.EnsurePositive);

        exception.Code.ShouldBe(DomainErrorCode.InvalidAmount);
    }

    [Fact]
    public void ToString_FormatsPaiseAsRupees()
    {
        new Money(123456, Currency.INR).ToString().ShouldBe("1234.56 INR");
    }
}
