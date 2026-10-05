namespace PaymentLedger.Domain.Transactions;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    HoldCapture,
    Refund,
    Reversal,
}
