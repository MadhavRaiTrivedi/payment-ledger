using Microsoft.AspNetCore.Mvc;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Api.Http;

internal static class LedgerResults
{
    private const string TransactionExtensionKey = "transaction";

    // A failed transaction is still recorded, so the client gets its id and reason along with the 422.
    public static IResult Transaction(IdempotentResult<TransactionResponse> result)
    {
        var transaction = result.Response;
        IResult inner = transaction.Status == TransactionStatus.Failed
            ? TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Transaction failed",
                Detail = $"The {transaction.Type} failed: {transaction.FailureReason}.",
                Extensions = { [TransactionExtensionKey] = transaction },
            })
            : TypedResults.Created($"/api/transactions/{transaction.Id}", transaction);

        return new IdempotentHttpResult(inner, result.IsReplay);
    }

    public static IResult Hold(IdempotentResult<HoldResponse> result, bool isNew) =>
        new IdempotentHttpResult(
            isNew ? TypedResults.Created($"/api/holds/{result.Response.Id}", result.Response) : TypedResults.Ok(result.Response),
            result.IsReplay);
}
