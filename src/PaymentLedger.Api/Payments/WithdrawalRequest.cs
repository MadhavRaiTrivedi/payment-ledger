using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Payments;

public sealed record WithdrawalRequest(Guid AccountId, [PositiveAmount] long AmountInPaise);
