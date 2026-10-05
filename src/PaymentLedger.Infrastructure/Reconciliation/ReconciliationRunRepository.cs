using Microsoft.EntityFrameworkCore;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Reconciliation;

internal sealed class ReconciliationRunRepository(LedgerDbContext db) : IReconciliationRunRepository
{
    public Task<ReconciliationRun?> FindLatestAsync(CancellationToken cancellationToken) =>
        db.ReconciliationRuns.OrderByDescending(run => run.StartedAt).FirstOrDefaultAsync(cancellationToken);

    public void Add(ReconciliationRun run) => db.ReconciliationRuns.Add(run);
}
