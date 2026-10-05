namespace PaymentLedger.Application.Transactions;

public sealed record RefundCommand(Guid TransactionId, long AmountInPaise);
