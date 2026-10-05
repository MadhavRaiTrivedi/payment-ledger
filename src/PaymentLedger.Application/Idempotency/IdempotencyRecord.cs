namespace PaymentLedger.Application.Idempotency;

public sealed record IdempotencyRecord(string RequestHash, string? ResponseBody);
