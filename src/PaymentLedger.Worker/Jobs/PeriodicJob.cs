namespace PaymentLedger.Worker.Jobs;

// Each tick runs in its own DI scope, so a job gets a fresh DbContext and one failed tick
// does not stop the next one.
internal abstract class PeriodicJob(IServiceScopeFactory scopeFactory, ILogger logger) : BackgroundService
{
    protected abstract TimeSpan Interval { get; }

    protected abstract Task RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await RunOnceAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "{Job} failed; retrying in {Interval}", GetType().Name, Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
