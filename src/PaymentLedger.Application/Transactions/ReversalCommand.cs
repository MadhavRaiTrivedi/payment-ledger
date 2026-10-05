namespace PaymentLedger.Application.Transactions;

public sealed record ReversalCommand(Guid TransactionId);
