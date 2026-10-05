namespace PaymentLedger.Application.Statements;

public interface IStatementReader
{
    Task<IReadOnlyList<StatementLine>> ReadLinesAsync(
        Guid accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        long? afterSequence,
        int limit,
        CancellationToken cancellationToken);

    Task<long> GetBalanceAsOfAsync(Guid accountId, DateTimeOffset asOf, CancellationToken cancellationToken);
}
