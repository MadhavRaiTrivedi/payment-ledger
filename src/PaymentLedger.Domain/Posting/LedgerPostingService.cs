using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Domain.Posting;

// System accounts take part in almost every transaction. Locking and updating their rows would make every
// request wait on the same row, so only customer wallets keep a balance snapshot. A system account's balance
// is the sum of its entries.
public sealed class LedgerPostingService
{
    public void Post(
        Transaction transaction,
        IReadOnlyCollection<Account> customerWallets,
        DateTimeOffset now,
        Hold? capturedHold = null)
    {
        var walletsById = customerWallets.ToDictionary(wallet => wallet.Id);
        var failureReason = FindFailureReason(transaction, walletsById, capturedHold);
        if (failureReason is not null)
        {
            transaction.MarkFailed(failureReason.Value, now);
            return;
        }

        capturedHold?.MarkCaptured(transaction.Id, GetWallet(walletsById, capturedHold.AccountId), now);

        foreach (var entry in transaction.Entries.Where(entry => !SystemAccounts.Contains(entry.AccountId)))
        {
            var balanceAfter = GetWallet(walletsById, entry.AccountId).Apply(entry.Direction, entry.Amount);
            entry.RecordBalanceAfter(balanceAfter);
        }

        transaction.MarkPosted(now);
    }

    private static TransactionFailureReason? FindFailureReason(
        Transaction transaction,
        Dictionary<Guid, Account> walletsById,
        Hold? capturedHold)
    {
        var touchedWallets = transaction.CustomerWalletIds.Select(accountId => GetWallet(walletsById, accountId));
        if (touchedWallets.Any(wallet => !wallet.IsActive))
        {
            return TransactionFailureReason.AccountNotActive;
        }

        var debitsByWallet = transaction.Entries
            .Where(entry => entry.Direction == EntryDirection.Debit && !SystemAccounts.Contains(entry.AccountId))
            .GroupBy(entry => entry.AccountId);

        foreach (var debits in debitsByWallet)
        {
            var wallet = GetWallet(walletsById, debits.Key);
            var available = wallet.AvailableBalance;
            if (capturedHold is not null && capturedHold.AccountId == wallet.Id)
            {
                available += capturedHold.Amount;
            }

            var totalDebit = debits.Aggregate(Money.Zero(wallet.Currency), (total, entry) => total + entry.Amount);
            if (totalDebit > available)
            {
                return TransactionFailureReason.InsufficientFunds;
            }
        }

        return null;
    }

    private static Account GetWallet(Dictionary<Guid, Account> walletsById, Guid accountId) =>
        walletsById.TryGetValue(accountId, out var wallet)
            ? wallet
            : throw new InvalidOperationException($"Wallet {accountId} must be loaded before posting.");
}
