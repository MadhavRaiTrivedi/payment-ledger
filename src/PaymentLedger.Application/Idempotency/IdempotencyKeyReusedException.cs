namespace PaymentLedger.Application.Idempotency;

public sealed class IdempotencyKeyReusedException(string key)
    : Exception($"Idempotency key '{key}' was already used for a different request.");
