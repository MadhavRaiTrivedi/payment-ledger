using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Domain.Transactions;

public sealed record TransactionPosted(
    Guid TransactionId,
    TransactionType Type,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    long AmountInPaise,
    long FeeInPaise,
    Currency Currency,
    DateTimeOffset OccurredAt) : IDomainEvent;
