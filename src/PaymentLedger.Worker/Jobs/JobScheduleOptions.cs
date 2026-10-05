namespace PaymentLedger.Worker.Jobs;

public sealed class JobScheduleOptions
{
    public const string SectionName = "Jobs";

    public TimeSpan HoldExpiryInterval { get; init; } = TimeSpan.FromSeconds(30);

    public int HoldExpiryBatchSize { get; init; } = 200;

    public TimeSpan ReconciliationInterval { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan IdempotencyCleanupInterval { get; init; } = TimeSpan.FromHours(1);
}
