using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Statements;

public sealed record StatementLine(
    long Sequence,
    Guid TransactionId,
    TransactionType TransactionType,
    EntryDirection Direction,
    long AmountInPaise,
    long? BalanceAfterInPaise,
    DateTimeOffset CreatedAt);
