using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    TransactionType Type,
    TransactionStatus Status,
    TransactionFailureReason? FailureReason,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    long AmountInPaise,
    long FeeInPaise,
    long RefundedInPaise,
    Currency Currency,
    Guid? OriginalTransactionId,
    Guid? ReversalTransactionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<EntryResponse> Entries)
{
    public static TransactionResponse From(Transaction transaction) => new(
        transaction.Id,
        transaction.Type,
        transaction.Status,
        transaction.FailureReason,
        transaction.SourceAccountId,
        transaction.DestinationAccountId,
        transaction.AmountInPaise,
        transaction.FeeInPaise,
        transaction.RefundedInPaise,
        transaction.Currency,
        transaction.OriginalTransactionId,
        transaction.ReversalTransactionId,
        transaction.CreatedAt,
        transaction.CompletedAt,
        transaction.Entries
            .OrderBy(entry => entry.Sequence)
            .Select(entry => new EntryResponse(
                entry.Sequence, entry.AccountId, entry.Direction, entry.AmountInPaise, entry.BalanceAfterInPaise))
            .ToList());
}
