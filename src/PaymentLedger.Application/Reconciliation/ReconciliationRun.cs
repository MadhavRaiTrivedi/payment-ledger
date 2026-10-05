namespace PaymentLedger.Application.Reconciliation;

public sealed class ReconciliationRun
{
    private ReconciliationRun()
    {
    }

    public Guid Id { get; private set; }

    public ReconciliationOutcome Outcome { get; private set; }

    public long LedgerTotalInPaise { get; private set; }

    public List<WalletMismatch> WalletMismatches { get; private set; } = [];

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }

    public static ReconciliationRun Record(LedgerCheckResult result, DateTimeOffset startedAt, DateTimeOffset completedAt)
    {
        var isBalanced = result.LedgerTotalInPaise == 0 && result.WalletMismatches.Count == 0;
        return new ReconciliationRun
        {
            Id = Guid.CreateVersion7(startedAt),
            Outcome = isBalanced ? ReconciliationOutcome.Balanced : ReconciliationOutcome.MismatchFound,
            LedgerTotalInPaise = result.LedgerTotalInPaise,
            WalletMismatches = [.. result.WalletMismatches],
            StartedAt = startedAt,
            CompletedAt = completedAt,
        };
    }
}
