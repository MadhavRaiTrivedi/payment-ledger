namespace PaymentLedger.Application.Accounts;

public sealed record ChangeAccountStatusCommand(Guid AccountId, AccountStatusChange Change);
