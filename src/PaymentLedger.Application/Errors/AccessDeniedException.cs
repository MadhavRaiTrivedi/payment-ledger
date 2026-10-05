namespace PaymentLedger.Application.Errors;

public sealed class AccessDeniedException(string message) : Exception(message);
