using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Holds;

public sealed record CaptureHoldRequest([PositiveAmount] long AmountInPaise);
