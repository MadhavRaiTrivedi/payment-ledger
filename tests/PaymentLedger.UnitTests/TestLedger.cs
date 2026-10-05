using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.UnitTests;

internal sealed class TestLedger
{
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly LedgerPostingService _posting = new();

    public Account Settlement { get; } =
        Account.CreateSystemAccount(SystemAccounts.SettlementId, AccountType.Settlement, Currency.INR, Now);

    public Account FeeRevenue { get; } =
        Account.CreateSystemAccount(SystemAccounts.FeeRevenueId, AccountType.FeeRevenue, Currency.INR, Now);

    public static Money Rupees(long rupees) => new(rupees * 100, Currency.INR);

    public static TransactionOrigin Origin() => new(Guid.NewGuid(), "test-correlation", null, Now);

    public Account OpenWallet(long rupees = 0)
    {
        var wallet = Account.OpenWallet(Guid.NewGuid(), Currency.INR, Now);
        if (rupees > 0)
        {
            Post(Transaction.Deposit(wallet, Rupees(rupees), Origin()), wallet);
        }

        return wallet;
    }

    public Transaction Post(Transaction transaction, params Account[] customerAccounts) =>
        Post(transaction, null, customerAccounts);

    public Transaction Post(Transaction transaction, Hold? capturedHold, params Account[] customerAccounts)
    {
        _posting.Post(transaction, [Settlement, FeeRevenue, .. customerAccounts], Now, capturedHold);
        return transaction;
    }
}
