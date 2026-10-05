namespace PaymentLedger.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "payment-ledger";

    public string Audience { get; init; } = "payment-ledger-api";

    public string SigningKey { get; init; } = string.Empty;

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(1);

    public bool EnableDevTokenEndpoint { get; init; }
}
