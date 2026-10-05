using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Security;

public static class RequestContextExtensions
{
    public static TransactionOrigin ToOrigin(this IRequestContext context, DateTimeOffset requestedAt) =>
        new(context.Requester.UserId, context.CorrelationId, context.IdempotencyKey, requestedAt);
}
