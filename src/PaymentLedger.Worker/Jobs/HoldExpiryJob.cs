using Microsoft.Extensions.Options;
using PaymentLedger.Application.Holds;

namespace PaymentLedger.Worker.Jobs;

internal sealed class HoldExpiryJob(
    IServiceScopeFactory scopeFactory,
    IOptions<JobScheduleOptions> options,
    ILogger<HoldExpiryJob> logger) : PeriodicJob(scopeFactory, logger)
{
    protected override TimeSpan Interval => options.Value.HoldExpiryInterval;

    protected override Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<ExpireHoldsHandler>().HandleAsync(options.Value.HoldExpiryBatchSize, cancellationToken);
}
