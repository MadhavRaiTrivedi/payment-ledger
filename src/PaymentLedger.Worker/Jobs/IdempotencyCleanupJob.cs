using Microsoft.Extensions.Options;
using PaymentLedger.Application.Idempotency;

namespace PaymentLedger.Worker.Jobs;

internal sealed class IdempotencyCleanupJob(
    IServiceScopeFactory scopeFactory,
    IOptions<JobScheduleOptions> options,
    TimeProvider clock,
    ILogger<IdempotencyCleanupJob> logger) : PeriodicJob(scopeFactory, logger)
{
    protected override TimeSpan Interval => options.Value.IdempotencyCleanupInterval;

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var deleted = await services.GetRequiredService<IIdempotencyStore>()
            .DeleteExpiredAsync(clock.GetUtcNow(), cancellationToken);
        if (deleted > 0)
        {
            logger.LogInformation("Deleted {KeyCount} expired idempotency keys", deleted);
        }
    }
}
