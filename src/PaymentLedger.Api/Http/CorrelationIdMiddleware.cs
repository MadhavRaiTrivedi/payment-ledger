using System.Diagnostics;

namespace PaymentLedger.Api.Http;

internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const int MaxCorrelationIdLength = 128;
    private static readonly object CorrelationIdItemKey = new();

    public static string? GetCorrelationId(HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdItemKey, out var value) ? value as string : null;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadOrCreate(context);
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers[LedgerHeaders.CorrelationId] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ReadOrCreate(HttpContext context)
    {
        var supplied = context.Request.Headers[LedgerHeaders.CorrelationId].ToString();
        if (!string.IsNullOrWhiteSpace(supplied) && supplied.Length <= MaxCorrelationIdLength)
        {
            return supplied;
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
