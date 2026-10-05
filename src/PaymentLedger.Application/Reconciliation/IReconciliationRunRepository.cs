namespace PaymentLedger.Application.Reconciliation;

public interface IReconciliationRunRepository
{
    Task<ReconciliationRun?> FindLatestAsync(CancellationToken cancellationToken);

    void Add(ReconciliationRun run);
}
