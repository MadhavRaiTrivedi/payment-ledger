namespace PaymentLedger.Domain.Transactions;

public enum TransactionStatus
{
    Pending,
    Posted,
    Failed,
    Reversed,
}
