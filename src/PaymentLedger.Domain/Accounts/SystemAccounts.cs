namespace PaymentLedger.Domain.Accounts;

public static class SystemAccounts
{
    public static readonly Guid SettlementId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid FeeRevenueId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SuspenseId = new("00000000-0000-0000-0000-000000000003");

    public static bool Contains(Guid accountId) =>
        accountId == SettlementId || accountId == FeeRevenueId || accountId == SuspenseId;
}
