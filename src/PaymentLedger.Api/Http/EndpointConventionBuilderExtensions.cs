namespace PaymentLedger.Api.Http;

internal static class EndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequiresIdempotencyKey(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(IdempotencyKeyRequirement.Instance);
}
