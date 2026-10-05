using Microsoft.Extensions.Options;
using PaymentLedger.Infrastructure.Outbox;

namespace PaymentLedger.Worker.Jobs;

internal sealed class OutboxPublishingJob(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublishingJob> logger) : PeriodicJob(scopeFactory, logger)
{
    protected override TimeSpan Interval => options.Value.PollInterval;

    protected override async Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var publisher = services.GetRequiredService<OutboxPublisher>();

        // Keep draining while full batches come back, instead of waiting a whole interval between them.
        int published;
        do
        {
            published = await publisher.PublishPendingAsync(cancellationToken);
        }
        while (published == options.Value.BatchSize);
    }
}
