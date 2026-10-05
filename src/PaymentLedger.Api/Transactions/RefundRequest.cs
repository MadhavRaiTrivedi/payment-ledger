using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.Transactions;

public sealed record RefundRequest([PositiveAmount] long AmountInPaise);
