namespace PaymentLedger.Application.Security;

public interface IRequestContext
{
    Requester Requester { get; }

    string? CorrelationId { get; }

    string? IdempotencyKey { get; }
}
