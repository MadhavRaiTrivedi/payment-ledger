namespace PaymentLedger.Application.Idempotency;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public TimeSpan KeyRetention { get; init; } = TimeSpan.FromHours(24);
}
