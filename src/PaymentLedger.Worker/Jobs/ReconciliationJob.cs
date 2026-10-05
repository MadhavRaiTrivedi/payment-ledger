using Microsoft.Extensions.Options;
using PaymentLedger.Application.Reconciliation;

namespace PaymentLedger.Worker.Jobs;

internal sealed class ReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<JobScheduleOptions> options,
    ILogger<ReconciliationJob> logger) : PeriodicJob(scopeFactory, logger)
{
    protected override TimeSpan Interval => options.Value.ReconciliationInterval;

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<ReconcileLedgerHandler>().HandleAsync(cancellationToken);
}
