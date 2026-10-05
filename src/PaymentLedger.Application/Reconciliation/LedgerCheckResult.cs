namespace PaymentLedger.Application.Reconciliation;

public sealed record LedgerCheckResult(long LedgerTotalInPaise, IReadOnlyList<WalletMismatch> WalletMismatches);
