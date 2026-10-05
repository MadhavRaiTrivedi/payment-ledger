using PaymentLedger.Domain;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Transactions;
using static PaymentLedger.UnitTests.TestLedger;

namespace PaymentLedger.UnitTests.Transactions;

public class TransactionTests
{
    private readonly TestLedger _ledger = new();

    [Fact]
    public void Transfer_WithFee_CreatesBalancedEntriesIncludingFeeRevenue()
    {
        var sender = _ledger.OpenWallet();
        var recipient = _ledger.OpenWallet();

        var transfer = Transaction.Transfer(sender, recipient, Rupees(500), Rupees(5), Origin());

        transfer.Status.ShouldBe(TransactionStatus.Pending);
        transfer.Entries.Count.ShouldBe(3);
        transfer.Entries.ShouldContain(entry =>
            entry.AccountId == sender.Id && entry.Direction == EntryDirection.Debit && entry.Amount == Rupees(505));
        transfer.Entries.ShouldContain(entry =>
            entry.AccountId == recipient.Id && entry.Direction == EntryDirection.Credit && entry.Amount == Rupees(500));
        transfer.Entries.ShouldContain(entry =>
            entry.AccountId == SystemAccounts.FeeRevenueId && entry.Amount == Rupees(5));
    }

    [Fact]
    public void Transfer_WithoutFee_HasNoFeeEntry()
    {
        var transfer = Transaction.Transfer(
            _ledger.OpenWallet(), _ledger.OpenWallet(), Rupees(100), Rupees(0), Origin());

        transfer.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public void Transfer_ToSameAccount_Throws()
    {
        var wallet = _ledger.OpenWallet();

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Transaction.Transfer(wallet, wallet, Rupees(100), Rupees(0), Origin()));

        exception.Code.ShouldBe(DomainErrorCode.InvalidCounterparty);
    }

    [Fact]
    public void Deposit_IntoSystemAccount_Throws()
    {
        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Transaction.Deposit(_ledger.FeeRevenue, Rupees(100), Origin()));

        exception.Code.ShouldBe(DomainErrorCode.InvalidCounterparty);
    }

    [Fact]
    public void Refund_MovesMoneyFromRecipientBackToSender()
    {
        var sender = _ledger.OpenWallet(rupees: 1000);
        var recipient = _ledger.OpenWallet();
        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(400), Rupees(0), Origin()), sender, recipient);

        var refund = Transaction.Refund(transfer, Rupees(150), Origin());

        refund.SourceAccountId.ShouldBe(recipient.Id);
        refund.DestinationAccountId.ShouldBe(sender.Id);
        refund.OriginalTransactionId.ShouldBe(transfer.Id);
    }

    [Fact]
    public void Refund_MoreThanRemainingAmount_Throws()
    {
        var sender = _ledger.OpenWallet(rupees: 1000);
        var recipient = _ledger.OpenWallet();
        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(400), Rupees(0), Origin()), sender, recipient);
        transfer.RecordRefund(Rupees(300));

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Transaction.Refund(transfer, Rupees(101), Origin()));

        exception.Code.ShouldBe(DomainErrorCode.RefundExceedsOriginal);
    }

    [Fact]
    public void Refund_OfDeposit_Throws()
    {
        var wallet = _ledger.OpenWallet();
        var deposit = _ledger.Post(Transaction.Deposit(wallet, Rupees(100), Origin()), wallet);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Transaction.Refund(deposit, Rupees(100), Origin()));

        exception.Code.ShouldBe(DomainErrorCode.TransactionNotRefundable);
    }

    [Fact]
    public void Reversal_MirrorsEveryEntryOfTheOriginal()
    {
        var sender = _ledger.OpenWallet(rupees: 1000);
        var recipient = _ledger.OpenWallet();
        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(200), Rupees(2), Origin()), sender, recipient);

        var reversal = Transaction.Reversal(transfer, Origin());

        reversal.Entries.Count.ShouldBe(transfer.Entries.Count);
        foreach (var original in transfer.Entries)
        {
            reversal.Entries.ShouldContain(mirrored =>
                mirrored.AccountId == original.AccountId
                && mirrored.Amount == original.Amount
                && mirrored.Direction != original.Direction);
        }
    }

    [Fact]
    public void Reversal_OfPartlyRefundedTransfer_Throws()
    {
        var sender = _ledger.OpenWallet(rupees: 1000);
        var recipient = _ledger.OpenWallet();
        var transfer = _ledger.Post(
            Transaction.Transfer(sender, recipient, Rupees(200), Rupees(0), Origin()), sender, recipient);
        transfer.RecordRefund(Rupees(50));

        var exception = Should.Throw<DomainRuleViolationException>(() => Transaction.Reversal(transfer, Origin()));

        exception.Code.ShouldBe(DomainErrorCode.TransactionNotReversible);
    }

    [Fact]
    public void MarkReversed_WhenPending_Throws()
    {
        var transfer = Transaction.Transfer(
            _ledger.OpenWallet(), _ledger.OpenWallet(), Rupees(100), Rupees(0), Origin());

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            transfer.MarkReversed(Guid.NewGuid(), Now));

        exception.Code.ShouldBe(DomainErrorCode.TransactionNotReversible);
    }

    [Fact]
    public void MarkReversed_WhenPosted_RaisesTransactionReversed()
    {
        var wallet = _ledger.OpenWallet();
        var deposit = _ledger.Post(Transaction.Deposit(wallet, Rupees(100), Origin()), wallet);
        var reversalId = Guid.NewGuid();

        deposit.MarkReversed(reversalId, Now);

        deposit.Status.ShouldBe(TransactionStatus.Reversed);
        deposit.DomainEvents.OfType<TransactionReversed>().ShouldHaveSingleItem()
            .ReversalTransactionId.ShouldBe(reversalId);
    }
}
