using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;

namespace PaymentLedger.Application.Reconciliation;

public sealed class GetLatestReconciliationHandler(IReconciliationRunRepository runs, IRequestContext requestContext)
{
    public async Task<ReconciliationRunResponse> HandleAsync(CancellationToken cancellationToken)
    {
        requestContext.Requester.EnsureAdmin();

        var latest = await runs.FindLatestAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(ReconciliationRun), "latest");
        return ReconciliationRunResponse.From(latest);
    }
}
