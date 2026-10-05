using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Payments;

public sealed record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, [PositiveAmount] long AmountInPaise);
