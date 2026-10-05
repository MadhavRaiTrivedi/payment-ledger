namespace PaymentLedger.Application.Statements;

public sealed record StatementQuery(
    Guid AccountId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    long? AfterSequence,
    int? Limit);
