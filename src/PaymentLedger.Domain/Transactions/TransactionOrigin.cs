namespace PaymentLedger.Domain.Transactions;

public sealed record TransactionOrigin(
    Guid InitiatedBy,
    string? CorrelationId,
    string? IdempotencyKey,
    DateTimeOffset RequestedAt);
