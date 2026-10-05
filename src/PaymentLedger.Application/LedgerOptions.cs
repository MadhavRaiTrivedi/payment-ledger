namespace PaymentLedger.Application;

public sealed class LedgerOptions
{
    public const string SectionName = "Ledger";

    public long TransferFeeInPaise { get; init; }

    public TimeSpan MaxHoldDuration { get; init; } = TimeSpan.FromDays(7);
}
