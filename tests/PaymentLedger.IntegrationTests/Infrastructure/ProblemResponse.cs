using PaymentLedger.Application.Transactions;

namespace PaymentLedger.IntegrationTests.Infrastructure;

internal sealed record ProblemResponse(int Status, string Title, string? Detail, string? Code, TransactionResponse? Transaction);
