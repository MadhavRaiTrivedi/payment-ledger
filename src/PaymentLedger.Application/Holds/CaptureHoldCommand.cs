namespace PaymentLedger.Application.Holds;

public sealed record CaptureHoldCommand(Guid HoldId, long AmountInPaise);
