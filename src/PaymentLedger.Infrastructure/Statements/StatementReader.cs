using System.Text;
using Dapper;
using Npgsql;
using PaymentLedger.Application.Statements;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Statements;

internal sealed class StatementReader(NpgsqlDataSource dataSource) : IStatementReader
{
    public async Task<IReadOnlyList<StatementLine>> ReadLinesAsync(
        Guid accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        long? afterSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder(
            """
            SELECT e.sequence, e.transaction_id, t.type AS transaction_type, e.direction,
                   e.amount_in_paise, e.balance_after_in_paise, e.created_at
            FROM entries e
            JOIN transactions t ON t.id = e.transaction_id
            WHERE e.account_id = @AccountId AND t.status = ANY(@SettledStatuses)
            """);
        if (from is not null)
        {
            sql.AppendLine(" AND e.created_at >= @From");
        }

        if (to is not null)
        {
            sql.AppendLine(" AND e.created_at < @To");
        }

        if (afterSequence is not null)
        {
            sql.AppendLine(" AND e.sequence > @AfterSequence");
        }

        sql.AppendLine(" ORDER BY e.sequence LIMIT @Limit");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StatementRow>(new CommandDefinition(
            sql.ToString(),
            new { AccountId = accountId, LedgerSql.SettledStatuses, From = from, To = to, AfterSequence = afterSequence, Limit = limit },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToStatementLine()).ToList();
    }

    public async Task<long> GetBalanceAsOfAsync(Guid accountId, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var parameters = new { AccountId = accountId, AsOf = asOf, LedgerSql.SettledStatuses, LedgerSql.Credit };

        // Wallet entries carry their running balance, so one indexed row answers the question.
        // System accounts have no running balance and are summed instead.
        var sql = SystemAccounts.Contains(accountId)
            ? $"""
              SELECT COALESCE(SUM({LedgerSql.SignedAmount}), 0)
              FROM entries e
              JOIN transactions t ON t.id = e.transaction_id
              WHERE e.account_id = @AccountId AND t.status = ANY(@SettledStatuses) AND e.created_at <= @AsOf
              """
            : """
              SELECT COALESCE((
                  SELECT e.balance_after_in_paise
                  FROM entries e
                  JOIN transactions t ON t.id = e.transaction_id
                  WHERE e.account_id = @AccountId AND t.status = ANY(@SettledStatuses) AND e.created_at <= @AsOf
                  ORDER BY e.sequence DESC
                  LIMIT 1), 0)
              """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private sealed class StatementRow
    {
        public long Sequence { get; init; }

        public Guid TransactionId { get; init; }

        public string TransactionType { get; init; } = string.Empty;

        public string Direction { get; init; } = string.Empty;

        public long AmountInPaise { get; init; }

        public long? BalanceAfterInPaise { get; init; }

        public DateTime CreatedAt { get; init; }

        public StatementLine ToStatementLine() => new(
            Sequence,
            TransactionId,
            Enum.Parse<TransactionType>(TransactionType),
            Enum.Parse<EntryDirection>(Direction),
            AmountInPaise,
            BalanceAfterInPaise,
            new DateTimeOffset(CreatedAt, TimeSpan.Zero));
    }
}
