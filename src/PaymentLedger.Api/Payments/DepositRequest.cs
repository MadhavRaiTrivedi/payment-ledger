using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Payments;

public sealed record DepositRequest(Guid AccountId, [PositiveAmount] long AmountInPaise);
