using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Application.Holds;

public sealed record HoldResponse(
    Guid Id,
    Guid AccountId,
    Guid BeneficiaryAccountId,
    long AmountInPaise,
    Currency Currency,
    HoldStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? CaptureTransactionId)
{
    public static HoldResponse From(Hold hold) => new(
        hold.Id,
        hold.AccountId,
        hold.BeneficiaryAccountId,
        hold.AmountInPaise,
        hold.Currency,
        hold.Status,
        hold.ExpiresAt,
        hold.CreatedAt,
        hold.CompletedAt,
        hold.CaptureTransactionId);
}
