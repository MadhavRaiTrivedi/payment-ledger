using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Domain.Posting;

public sealed class LedgerPostingService
{
    public void Post(Transaction transaction, IReadOnlyCollection<Account> accounts, DateTimeOffset now, Hold? capturedHold = null)
    {
        var accountsById = accounts.ToDictionary(account => account.Id);
        var failureReason = FindFailureReason(transaction, accountsById, capturedHold);
        if (failureReason is not null)
        {
            transaction.MarkFailed(failureReason.Value, now);
            return;
        }

        capturedHold?.MarkCaptured(transaction.Id, GetAccount(accountsById, capturedHold.AccountId), now);

        foreach (var entry in transaction.Entries)
        {
            var balanceAfter = GetAccount(accountsById, entry.AccountId).Apply(entry.Direction, entry.Amount);
            entry.RecordBalanceAfter(balanceAfter);
        }

        transaction.MarkPosted(now);
    }

    private static TransactionFailureReason? FindFailureReason(
        Transaction transaction,
        Dictionary<Guid, Account> accountsById,
        Hold? capturedHold)
    {
        var touchedAccounts = transaction.AccountIds.Select(id => GetAccount(accountsById, id)).ToList();
        if (touchedAccounts.Any(account => !account.IsActive))
        {
            return TransactionFailureReason.AccountNotActive;
        }

        var debitsByAccount = transaction.Entries
            .Where(entry => entry.Direction == EntryDirection.Debit)
            .GroupBy(entry => entry.AccountId);

        foreach (var debits in debitsByAccount)
        {
            var account = GetAccount(accountsById, debits.Key);
            if (account.CanGoNegative)
            {
                continue;
            }

            var available = account.AvailableBalance;
            if (capturedHold is not null && capturedHold.AccountId == account.Id)
            {
                available += capturedHold.Amount;
            }

            var totalDebit = debits.Aggregate(Money.Zero(account.Currency), (total, entry) => total + entry.Amount);
            if (totalDebit > available)
            {
                return TransactionFailureReason.InsufficientFunds;
            }
        }

        return null;
    }

    private static Account GetAccount(Dictionary<Guid, Account> accountsById, Guid accountId) =>
        accountsById.TryGetValue(accountId, out var account)
            ? account
            : throw new InvalidOperationException($"Account {accountId} must be loaded before posting.");
}
