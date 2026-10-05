using Microsoft.Extensions.Logging;
using PaymentLedger.Application.Observability;
using PaymentLedger.Application.Persistence;

namespace PaymentLedger.Application.Reconciliation;

public sealed class ReconcileLedgerHandler(
    ILedgerChecks checks,
    IReconciliationRunRepository runs,
    IUnitOfWork unitOfWork,
    LedgerMetrics metrics,
    TimeProvider clock,
    ILogger<ReconcileLedgerHandler> logger)
{
    public async Task<ReconciliationRunResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var startedAt = clock.GetUtcNow();
        var result = await checks.RunAsync(cancellationToken);
        var run = ReconciliationRun.Record(result, startedAt, clock.GetUtcNow());

        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        metrics.ReconciliationCompleted(run);

        if (run.Outcome == ReconciliationOutcome.MismatchFound)
        {
            logger.LogError(
                "Reconciliation {RunId} found a mismatch: ledger total {LedgerTotalInPaise} paise, {MismatchedWalletCount} wallets out of balance",
                run.Id,
                run.LedgerTotalInPaise,
                run.WalletMismatches.Count);
        }
        else
        {
            logger.LogInformation("Reconciliation {RunId} balanced", run.Id);
        }

        return ReconciliationRunResponse.From(run);
    }
}
