namespace PaymentLedger.Api.Security;

public sealed record DevTokenResponse(string AccessToken, Guid UserId, UserRole Role, DateTimeOffset ExpiresAt);
