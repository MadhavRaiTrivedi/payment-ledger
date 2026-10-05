namespace PaymentLedger.Domain.Transactions;

public sealed record TransactionFailed(
    Guid TransactionId,
    TransactionType Type,
    TransactionFailureReason Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
