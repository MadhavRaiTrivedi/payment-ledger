using PaymentLedger.Api.Security;
using PaymentLedger.Application.Reconciliation;

namespace PaymentLedger.Api.Reconciliation;

internal static class ReconciliationEndpoints
{
    public static void MapReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        var runs = app.MapGroup("/api/reconciliation-runs")
            .WithTags("Reconciliation")
            .RequireAuthorization(AuthorizationPolicies.Admin);

        runs.MapGet("/latest", (GetLatestReconciliationHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(cancellationToken));
        runs.MapPost("/", async (ReconcileLedgerHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));
    }
}
