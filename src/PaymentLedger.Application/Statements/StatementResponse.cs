namespace PaymentLedger.Application.Statements;

public sealed record StatementResponse(Guid AccountId, IReadOnlyList<StatementLine> Lines, long? NextCursor);
