namespace PaymentLedger.Infrastructure.Messaging;

public sealed class ProcessedMessage
{
    public Guid MessageId { get; init; }

    public string Consumer { get; init; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; init; }
}
