namespace PaymentLedger.Application.Holds;

public sealed record PlaceHoldCommand(
    Guid AccountId,
    Guid BeneficiaryAccountId,
    long AmountInPaise,
    DateTimeOffset ExpiresAt);
