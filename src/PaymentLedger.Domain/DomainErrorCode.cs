namespace PaymentLedger.Domain;

public enum DomainErrorCode
{
    InvalidAmount,
    CurrencyMismatch,
    UnbalancedTransaction,
    InvalidStatusTransition,
    InvalidCounterparty,
    AccountNotActive,
    SystemAccountStatusChange,
    AccountNotEmpty,
    InsufficientFunds,
    TransactionNotRefundable,
    RefundExceedsOriginal,
    TransactionNotReversible,
    HoldNotActive,
    HoldExpired,
    HoldNotYetExpired,
    CaptureExceedsHold,
    InvalidHoldExpiry,
}
