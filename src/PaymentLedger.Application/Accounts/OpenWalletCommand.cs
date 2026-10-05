using PaymentLedger.Domain.Currencies;

namespace PaymentLedger.Application.Accounts;

public sealed record OpenWalletCommand(Guid OwnerId, Currency Currency);
