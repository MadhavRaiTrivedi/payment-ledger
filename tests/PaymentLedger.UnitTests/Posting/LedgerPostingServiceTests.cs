using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Transactions;
using static PaymentLedger.UnitTests.TestLedger;

namespace PaymentLedger.UnitTests.Posting;

public class LedgerPostingServiceTests
{
    private readonly TestLedger _ledger = new();

    [Fact]
    public void Post_Deposit_CreditsWalletAndTakesSettlementNegative()
    {
        var wallet = _ledger.OpenWallet();

        var deposit = _ledger.Post(Transaction.Deposit(wallet, Rupees(1000), Origin()), wallet);

        deposit.Status.ShouldBe(TransactionStatus.Posted);
        wallet.Balance.ShouldBe(Rupees(1000));
        _ledger.DerivedBalance(SystemAccounts.SettlementId).ShouldBe(Rupees(-1000));
    }

    [Fact]
    public void Post_DoesNotNeedSystemAccountsLoaded()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);

        var withdrawal = _ledger.Post(Transaction.Withdrawal(wallet, Rupees(40), Origin()), wallet);

        withdrawal.Status.ShouldBe(TransactionStatus.Posted);
        withdrawal.Entries.Single(entry => entry.AccountId == SystemAccounts.SettlementId)
            .BalanceAfterInPaise.ShouldBeNull();
    }

    [Fact]
    public void Post_Transfer_RecordsRunningBalanceOnEachEntry()
    {
        var sender = _ledger.OpenWallet(rupees: 1000);
        var recipient = _ledger.OpenWallet(rupees: 50);

        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(300), Rupees(5), Origin()), sender, recipient);

        transfer.Status.ShouldBe(TransactionStatus.Posted);
        transfer.Entries.Single(entry => entry.AccountId == sender.Id).BalanceAfterInPaise.ShouldBe(Rupees(695).AmountInPaise);
        transfer.Entries.Single(entry => entry.AccountId == recipient.Id).BalanceAfterInPaise.ShouldBe(Rupees(350).AmountInPaise);
        _ledger.DerivedBalance(SystemAccounts.FeeRevenueId).ShouldBe(Rupees(5));
    }

    [Fact]
    public void Post_WhenSenderCannotCoverAmountPlusFee_FailsWithoutMovingMoney()
    {
        var sender = _ledger.OpenWallet(rupees: 100);
        var recipient = _ledger.OpenWallet();

        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(100), Rupees(1), Origin()), sender, recipient);

        transfer.Status.ShouldBe(TransactionStatus.Failed);
        transfer.FailureReason.ShouldBe(TransactionFailureReason.InsufficientFunds);
        sender.Balance.ShouldBe(Rupees(100));
        recipient.Balance.ShouldBe(Rupees(0));
        transfer.Entries.ShouldAllBe(entry => entry.BalanceAfterInPaise == null);
        transfer.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TransactionFailed>();
    }

    [Fact]
    public void Post_WhenRecipientIsFrozen_FailsWithAccountNotActive()
    {
        var sender = _ledger.OpenWallet(rupees: 100);
        var recipient = _ledger.OpenWallet();
        recipient.Freeze();

        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(10), Rupees(0), Origin()), sender, recipient);

        transfer.FailureReason.ShouldBe(TransactionFailureReason.AccountNotActive);
        sender.Balance.ShouldBe(Rupees(100));
    }

    [Fact]
    public void Post_WhenFundsAreOnHold_CannotSpendThem()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);
        var merchant = _ledger.OpenWallet();
        Hold.Place(wallet, merchant, Rupees(80), Now.AddHours(1), Now);

        var withdrawal = _ledger.Post(Transaction.Withdrawal(wallet, Rupees(30), Origin()), wallet);

        withdrawal.FailureReason.ShouldBe(TransactionFailureReason.InsufficientFunds);
    }

    [Fact]
    public void Post_HoldCapture_SpendsHeldFundsAndReleasesTheRest()
    {
        var wallet = _ledger.OpenWallet(rupees: 100);
        var merchant = _ledger.OpenWallet();
        var hold = Hold.Place(wallet, merchant, Rupees(100), Now.AddHours(1), Now);

        var capture = _ledger.Post(Transaction.HoldCapture(hold, Rupees(60), Origin()), hold, wallet, merchant);

        capture.Status.ShouldBe(TransactionStatus.Posted);
        hold.Status.ShouldBe(HoldStatus.Captured);
        hold.CaptureTransactionId.ShouldBe(capture.Id);
        wallet.Balance.ShouldBe(Rupees(40));
        wallet.AvailableBalance.ShouldBe(Rupees(40));
        merchant.Balance.ShouldBe(Rupees(60));
    }

    [Fact]
    public void Post_ManyTransactions_KeepsLedgerAtZero()
    {
        var alice = _ledger.OpenWallet(rupees: 1000);
        var bob = _ledger.OpenWallet(rupees: 200);

        _ledger.Post(Transaction.Transfer(alice, bob, Rupees(250), Rupees(3), Origin()), alice, bob);
        _ledger.Post(Transaction.Withdrawal(bob, Rupees(100), Origin()), bob);
        _ledger.Post(Transaction.Transfer(bob, alice, Rupees(5000), Rupees(3), Origin()), alice, bob);

        var total = alice.Balance
            + bob.Balance
            + _ledger.DerivedBalance(SystemAccounts.SettlementId)
            + _ledger.DerivedBalance(SystemAccounts.FeeRevenueId);
        total.IsZero.ShouldBeTrue();
    }
}
