using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Domain;

namespace PaymentLedger.Api.Http;

internal sealed class LedgerExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string ErrorCodeExtensionKey = "code";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = ToProblem(exception);
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblem(Exception exception) => exception switch
    {
        DomainRuleViolationException violation => new ProblemDetails
        {
            Status = violation.Code == DomainErrorCode.InvalidStatusTransition
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status422UnprocessableEntity,
            Title = "Ledger rule violated",
            Detail = violation.Message,
            Extensions = { [ErrorCodeExtensionKey] = violation.Code.ToString() },
        },
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = $"{notFound.Resource} not found",
            Detail = notFound.Message,
        },
        AccessDeniedException accessDenied => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access denied",
            Detail = accessDenied.Message,
        },
        InvalidRequestException invalidRequest => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request",
            Detail = invalidRequest.Message,
        },
        IdempotencyKeyReusedException reused => new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Idempotency key reused",
            Detail = reused.Message,
        },
        _ => null,
    };
}
