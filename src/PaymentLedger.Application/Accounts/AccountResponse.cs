using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Application.Accounts;

public sealed record AccountResponse(
    Guid Id,
    AccountType Type,
    AccountStatus Status,
    Guid? OwnerId,
    Currency Currency,
    long BalanceInPaise,
    long HeldInPaise,
    long AvailableInPaise,
    DateTimeOffset OpenedAt)
{
    public static AccountResponse From(Account account) => new(
        account.Id,
        account.Type,
        account.Status,
        account.OwnerId,
        account.Currency,
        account.BalanceInPaise,
        account.HeldInPaise,
        account.AvailableBalance.AmountInPaise,
        account.OpenedAt);
}
