using PaymentLedger.Api.Http;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Idempotency;

namespace PaymentLedger.Api.Holds;

internal static class HoldEndpoints
{
    public static void MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        var holds = app.MapGroup("/api/holds").WithTags("Holds").RequireAuthorization();

        holds.MapPost("/", PlaceAsync).RequiresIdempotencyKey();
        holds.MapGet("/{holdId:guid}", GetAsync);
        holds.MapPost("/{holdId:guid}/capture", CaptureAsync).RequiresIdempotencyKey();
        holds.MapPost("/{holdId:guid}/release", ReleaseAsync).RequiresIdempotencyKey();
    }

    private static async Task<IResult> PlaceAsync(
        PlaceHoldRequest request,
        IdempotentCommandRunner runner,
        PlaceHoldHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new PlaceHoldCommand(
            request.AccountId, request.BeneficiaryAccountId, request.AmountInPaise, request.ExpiresAt);
        return LedgerResults.Hold(await runner.RunAsync(command, handler.HandleAsync, cancellationToken), isNew: true);
    }

    private static Task<HoldResponse> GetAsync(Guid holdId, GetHoldHandler handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(holdId, cancellationToken);

    private static async Task<IResult> CaptureAsync(
        Guid holdId,
        CaptureHoldRequest request,
        IdempotentCommandRunner runner,
        CaptureHoldHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Transaction(await runner.RunAsync(
            new CaptureHoldCommand(holdId, request.AmountInPaise), handler.HandleAsync, cancellationToken));

    private static async Task<IResult> ReleaseAsync(
        Guid holdId,
        IdempotentCommandRunner runner,
        ReleaseHoldHandler handler,
        CancellationToken cancellationToken) =>
        LedgerResults.Hold(
            await runner.RunAsync(new ReleaseHoldCommand(holdId), handler.HandleAsync, cancellationToken), isNew: false);
}
