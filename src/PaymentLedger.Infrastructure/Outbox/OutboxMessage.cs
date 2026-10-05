namespace PaymentLedger.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private const int MaxErrorLength = 2000;

    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int PublishAttempts { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(string type, string payload, DateTimeOffset occurredAt) => new()
    {
        Id = Guid.CreateVersion7(occurredAt),
        Type = type,
        Payload = payload,
        OccurredAt = occurredAt,
    };

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        PublishAttempts++;
        PublishedAt = publishedAt;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        PublishAttempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
