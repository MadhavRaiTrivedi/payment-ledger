using PaymentLedger.Domain;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.UnitTests.Accounts;

public class AccountTests
{
    private readonly TestLedger _ledger = new();

    [Fact]
    public void Close_WhenBalanceIsNotZero_Throws()
    {
        var wallet = _ledger.OpenWallet(rupees: 10);

        var exception = Should.Throw<DomainRuleViolationException>(wallet.Close);

        exception.Code.ShouldBe(DomainErrorCode.AccountNotEmpty);
        wallet.Status.ShouldBe(AccountStatus.Active);
    }

    [Fact]
    public void Close_WhenEmpty_ClosesAccount()
    {
        var wallet = _ledger.OpenWallet();

        wallet.Close();

        wallet.Status.ShouldBe(AccountStatus.Closed);
    }

    [Fact]
    public void Unfreeze_WhenClosed_Throws()
    {
        var wallet = _ledger.OpenWallet();
        wallet.Close();

        var exception = Should.Throw<DomainRuleViolationException>(wallet.Unfreeze);

        exception.Code.ShouldBe(DomainErrorCode.InvalidStatusTransition);
    }

    [Fact]
    public void Freeze_OnSystemAccount_Throws()
    {
        var exception = Should.Throw<DomainRuleViolationException>(_ledger.Settlement.Freeze);

        exception.Code.ShouldBe(DomainErrorCode.SystemAccountStatusChange);
    }
}
