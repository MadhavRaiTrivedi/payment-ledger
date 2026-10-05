using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public sealed record EntryResponse(
    long Sequence,
    Guid AccountId,
    EntryDirection Direction,
    long AmountInPaise,
    long? BalanceAfterInPaise);
