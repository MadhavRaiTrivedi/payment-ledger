namespace PaymentLedger.Application.Reconciliation;

public sealed record WalletMismatch(
    Guid AccountId,
    long SnapshotBalanceInPaise,
    long DerivedBalanceInPaise,
    long SnapshotHeldInPaise,
    long DerivedHeldInPaise);
