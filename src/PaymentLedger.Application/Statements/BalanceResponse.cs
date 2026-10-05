using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Application.Statements;

public sealed record BalanceResponse(Guid AccountId, DateTimeOffset AsOf, long BalanceInPaise, Currency Currency);
