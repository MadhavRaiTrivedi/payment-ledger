using System.Security.Claims;
using PaymentLedger.Api.Http;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;

namespace PaymentLedger.Api.Security;

internal sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("There is no active HTTP request.");

    public Requester Requester
    {
        get
        {
            var user = HttpContext.User;
            var subject = user.FindFirstValue(LedgerClaimTypes.UserId);
            if (!Guid.TryParse(subject, out var userId))
            {
                throw new AccessDeniedException("The access token has no valid subject.");
            }

            return new Requester(userId, user.IsInRole(nameof(UserRole.Admin)));
        }
    }

    public string? CorrelationId => CorrelationIdMiddleware.GetCorrelationId(HttpContext);

    public string? IdempotencyKey
    {
        get
        {
            var key = HttpContext.Request.Headers[LedgerHeaders.IdempotencyKey].ToString();
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }
    }
}
