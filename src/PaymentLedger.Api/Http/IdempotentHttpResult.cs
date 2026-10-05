namespace PaymentLedger.Api.Http;

internal sealed class IdempotentHttpResult(IResult inner, bool isReplay) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (isReplay)
        {
            httpContext.Response.Headers[LedgerHeaders.IdempotentReplayed] = bool.TrueString.ToLowerInvariant();
        }

        return inner.ExecuteAsync(httpContext);
    }
}
