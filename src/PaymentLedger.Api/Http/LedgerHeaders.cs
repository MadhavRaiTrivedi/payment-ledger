namespace PaymentLedger.Api.Http;

public static class LedgerHeaders
{
    public const string IdempotencyKey = "Idempotency-Key";
    public const string IdempotentReplayed = "Idempotent-Replayed";
    public const string CorrelationId = "X-Correlation-Id";
}
