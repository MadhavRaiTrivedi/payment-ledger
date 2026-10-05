namespace PaymentLedger.Application.Payments;

public sealed record DepositCommand(Guid AccountId, long AmountInPaise);
