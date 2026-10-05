namespace PaymentLedger.Application.Payments;

public sealed record TransferCommand(Guid SourceAccountId, Guid DestinationAccountId, long AmountInPaise);
