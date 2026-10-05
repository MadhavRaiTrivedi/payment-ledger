namespace PaymentLedger.Domain.Transactions;

public sealed record TransactionReversed(
    Guid TransactionId,
    Guid ReversalTransactionId,
    DateTimeOffset OccurredAt) : IDomainEvent;
