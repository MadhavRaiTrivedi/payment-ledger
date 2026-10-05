namespace PaymentLedger.Application.Payments;

public sealed record WithdrawalCommand(Guid AccountId, long AmountInPaise);
