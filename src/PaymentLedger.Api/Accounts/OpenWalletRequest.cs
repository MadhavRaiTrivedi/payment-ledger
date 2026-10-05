using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Api.Accounts;

public sealed record OpenWalletRequest(Guid OwnerId, Currency Currency = Currency.INR);
