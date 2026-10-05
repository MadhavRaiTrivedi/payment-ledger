namespace PaymentLedger.Application.Idempotency;

public sealed record IdempotencyClaim(
    Guid OwnerId,
    string Key,
    string RequestHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
