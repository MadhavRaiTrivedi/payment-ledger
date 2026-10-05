namespace PaymentLedger.Api.Http;

internal sealed class IdempotencyKeyRequirement
{
    public static readonly IdempotencyKeyRequirement Instance = new();
}
