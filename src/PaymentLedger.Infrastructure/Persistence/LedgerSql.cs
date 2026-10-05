using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Infrastructure.Persistence;

internal static class LedgerSql
{
    // Reversed transactions stay in the ledger: their reversal is a separate transaction with opposite entries.
    public static readonly string[] SettledStatuses =
    [
        TransactionStatus.Posted.ToString(),
        TransactionStatus.Reversed.ToString(),
    ];

    public static readonly string Credit = EntryDirection.Credit.ToString();

    public const string SignedAmount =
        "CASE WHEN e.direction = @Credit THEN e.amount_in_paise ELSE -e.amount_in_paise END";
}
