using Microsoft.IdentityModel.JsonWebTokens;

namespace PaymentLedger.Api.Security;

public static class LedgerClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Role = "role";
}
