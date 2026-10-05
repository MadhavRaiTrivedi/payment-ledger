using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Holds;

public sealed record PlaceHoldRequest(
    Guid AccountId,
    Guid BeneficiaryAccountId,
    [PositiveAmount] long AmountInPaise,
    DateTimeOffset ExpiresAt);
