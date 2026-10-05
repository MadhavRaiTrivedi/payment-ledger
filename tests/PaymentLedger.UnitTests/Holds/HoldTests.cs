using PaymentLedger.Domain;
using PaymentLedger.Domain.Holds;
using static PaymentLedger.UnitTests.TestLedger;

namespace PaymentLedger.UnitTests.Holds;

public class HoldTests
{
    private readonly TestLedger _ledger = new();

    [Fact]
    public void Place_ReservesFundsWithoutChangingBalance()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);

        Hold.Place(wallet, _ledger.OpenWallet(), Rupees(70), Now.AddHours(1), Now);

        wallet.Balance.ShouldBe(Rupees(100));
        wallet.AvailableBalance.ShouldBe(Rupees(30));
    }

    [Fact]
    public void Place_MoreThanAvailable_Throws()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Hold.Place(wallet, _ledger.OpenWallet(), Rupees(101), Now.AddHours(1), Now));

        exception.Code.ShouldBe(DomainErrorCode.InsufficientFunds);
    }

    [Fact]
    public void Place_WithPastExpiry_Throws()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Hold.Place(wallet, _ledger.OpenWallet(), Rupees(10), Now, Now));

        exception.Code.ShouldBe(DomainErrorCode.InvalidHoldExpiry);
    }

    [Fact]
    public void EnsureCapturable_AfterExpiry_Throws()
    {
        var hold = Hold.Place(_ledger.OpenWallet(rupees: 100), _ledger.OpenWallet(), Rupees(10), Now.AddMinutes(5), Now);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            hold.EnsureCapturable(Rupees(10), Now.AddMinutes(5)));

        exception.Code.ShouldBe(DomainErrorCode.HoldExpired);
    }

    [Fact]
    public void EnsureCapturable_MoreThanHeld_Throws()
    {
        var hold = Hold.Place(_ledger.OpenWallet(rupees: 100), _ledger.OpenWallet(), Rupees(10), Now.AddMinutes(5), Now);

        var exception = Should.Throw<DomainRuleViolationException>(() => hold.EnsureCapturable(Rupees(11), Now));

        exception.Code.ShouldBe(DomainErrorCode.CaptureExceedsHold);
    }

    [Fact]
    public void Release_MakesFundsAvailableAgain()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);
        var hold = Hold.Place(wallet, _ledger.OpenWallet(), Rupees(70), Now.AddHours(1), Now);

        hold.Release(wallet, Now);

        hold.Status.ShouldBe(HoldStatus.Released);
        wallet.AvailableBalance.ShouldBe(Rupees(100));
    }

    [Fact]
    public void Expire_BeforeExpiryTime_Throws()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);
        var hold = Hold.Place(wallet, _ledger.OpenWallet(), Rupees(70), Now.AddHours(1), Now);

        var exception = Should.Throw<DomainRuleViolationException>(() => hold.Expire(wallet, Now));

        exception.Code.ShouldBe(DomainErrorCode.HoldNotYetExpired);
    }

    [Fact]
    public void Release_Twice_Throws()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);
        var hold = Hold.Place(wallet, _ledger.OpenWallet(), Rupees(70), Now.AddHours(1), Now);
        hold.Release(wallet, Now);

        var exception = Should.Throw<DomainRuleViolationException>(() => hold.Release(wallet, Now));

        exception.Code.ShouldBe(DomainErrorCode.InvalidStatusTransition);
        wallet.AvailableBalance.ShouldBe(Rupees(100));
    }
}
