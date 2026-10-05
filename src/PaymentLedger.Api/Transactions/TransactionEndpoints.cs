using PaymentLedger.Api.Http;
using PaymentLedger.Api.Security;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Application.Transactions;

namespace PaymentLedger.Api.Transactions;

internal static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var transactions = app.MapGroup("/api/transactions").WithTags("Transactions").RequireAuthorization();

        transactions.MapGet("/{transactionId:guid}", GetAsync);
        transactions.MapPost("/{transactionId:guid}/refunds", RefundAsync)
            .RequiresIdempotencyKey()
            .RequireAuthorization(AuthorizationPolicies.Admin);
        transactions.MapPost("/{transactionId:guid}/reversal", ReverseAsync)
            .RequiresIdempotencyKey()
            .RequireAuthorization(AuthorizationPolicies.Admin);
    }

    private static Task<TransactionResponse> GetAsync(
        Guid transactionId,
        GetTransactionHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(transactionId, cancellationToken);

    private static async Task<IResult> RefundAsync(
        Guid transactionId,
        RefundRequest request,
        IdempotentCommandRunner runner,
        RefundHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new RefundCommand(transactionId, request.AmountInPaise), handler.HandleAsync, cancellationToken));

    private static async Task<IResult> ReverseAsync(
        Guid transactionId,
        IdempotentCommandRunner runner,
        ReversalHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new ReversalCommand(transactionId), handler.HandleAsync, cancellationToken));
}
