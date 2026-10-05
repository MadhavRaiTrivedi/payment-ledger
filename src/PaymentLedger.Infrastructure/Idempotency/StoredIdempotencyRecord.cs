namespace PaymentLedger.Infrastructure.Idempotency;

public sealed class StoredIdempotencyRecord
{
    public Guid OwnerId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public string RequestHash { get; init; } = string.Empty;

    public string? ResponseBody { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }
}
