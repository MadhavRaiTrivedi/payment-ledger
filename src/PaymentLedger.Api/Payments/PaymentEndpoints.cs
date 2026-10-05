using PaymentLedger.Api.Http;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Application.Payments;

namespace PaymentLedger.Api.Payments;

internal static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var payments = app.MapGroup("/api").WithTags("Payments").RequireAuthorization();

        payments.MapPost("/deposits", DepositAsync).RequiresIdempotencyKey();
        payments.MapPost("/withdrawals", WithdrawAsync).RequiresIdempotencyKey();
        payments.MapPost("/transfers", TransferAsync).RequiresIdempotencyKey();
    }

    private static async Task<IResult> DepositAsync(
        DepositRequest request,
        IdempotentCommandRunner runner,
        DepositHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new DepositCommand(request.AccountId, request.AmountInPaise), handler.HandleAsync, cancellationToken));

    private static async Task<IResult> WithdrawAsync(
        WithdrawalRequest request,
        IdempotentCommandRunner runner,
        WithdrawalHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new WithdrawalCommand(request.AccountId, request.AmountInPaise), handler.HandleAsync, cancellationToken));

    private static async Task<IResult> TransferAsync(
        TransferRequest request,
        IdempotentCommandRunner runner,
        TransferHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new TransferCommand(request.SourceAccountId, request.DestinationAccountId, request.AmountInPaise),
            handler.HandleAsync,
            cancellationToken));
}
