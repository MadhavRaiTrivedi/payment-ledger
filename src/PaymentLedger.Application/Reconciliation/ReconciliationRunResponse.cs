namespace PaymentLedger.Application.Reconciliation;

public sealed record ReconciliationRunResponse(
    Guid Id,
    ReconciliationOutcome Outcome,
    long LedgerTotalInPaise,
    IReadOnlyList<WalletMismatch> WalletMismatches,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public static ReconciliationRunResponse From(ReconciliationRun run) => new(
        run.Id, run.Outcome, run.LedgerTotalInPaise, run.WalletMismatches, run.StartedAt, run.CompletedAt);
}
