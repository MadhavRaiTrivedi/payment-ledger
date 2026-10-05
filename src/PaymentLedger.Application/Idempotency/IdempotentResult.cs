namespace PaymentLedger.Application.Idempotency;

public sealed record IdempotentResult<TResponse>(TResponse Response, bool IsReplay);
