namespace PaymentLedger.Api.Security;

public sealed record DevTokenRequest(Guid? UserId, UserRole Role);
