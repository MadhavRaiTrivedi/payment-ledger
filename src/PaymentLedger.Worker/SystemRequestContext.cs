using PaymentLedger.Application.Security;

namespace PaymentLedger.Worker;

internal sealed class SystemRequestContext : IRequestContext
{
    public Requester Requester => Requester.System;

    public string? CorrelationId => null;

    public string? IdempotencyKey => null;
}
