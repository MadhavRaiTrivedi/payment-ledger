namespace PaymentLedger.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    public int BatchSize { get; init; } = 100;

    public int MaxPublishAttempts { get; init; } = 10;
}
