namespace PaymentLedger.Application.Errors;

public sealed class InvalidRequestException(string message) : Exception(message);
