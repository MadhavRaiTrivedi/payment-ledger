using System.Data;
using Dapper;
using Npgsql;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Infrastructure.Persistence;

namespace PaymentLedger.Infrastructure.Reconciliation;

internal sealed class LedgerChecks(NpgsqlDataSource dataSource) : ILedgerChecks
{
    private const string LedgerTotalSql =
        $"""
        SELECT COALESCE(SUM({LedgerSql.SignedAmount}), 0)
        FROM entries e
        JOIN transactions t ON t.id = e.transaction_id
        WHERE t.status = ANY(@SettledStatuses)
        """;

    private const string WalletMismatchesSql =
        $"""
        WITH derived AS (
            SELECT e.account_id, SUM({LedgerSql.SignedAmount}) AS balance
            FROM entries e
            JOIN transactions t ON t.id = e.transaction_id
            WHERE t.status = ANY(@SettledStatuses)
            GROUP BY e.account_id
        ),
        held AS (
            SELECT account_id, SUM(amount_in_paise) AS held
            FROM holds
            WHERE status = @ActiveHold
            GROUP BY account_id
        )
        SELECT a.id AS account_id,
               a.balance_in_paise AS snapshot_balance_in_paise,
               COALESCE(d.balance, 0) AS derived_balance_in_paise,
               a.held_in_paise AS snapshot_held_in_paise,
               COALESCE(h.held, 0) AS derived_held_in_paise
        FROM accounts a
        LEFT JOIN derived d ON d.account_id = a.id
        LEFT JOIN held h ON h.account_id = a.id
        WHERE a.type = @CustomerWallet
          AND (a.balance_in_paise <> COALESCE(d.balance, 0) OR a.held_in_paise <> COALESCE(h.held, 0))
        ORDER BY a.id
        """;

    public async Task<LedgerCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        var parameters = new
        {
            LedgerSql.SettledStatuses,
            LedgerSql.Credit,
            ActiveHold = HoldStatus.Active.ToString(),
            CustomerWallet = AccountType.CustomerWallet.ToString(),
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var snapshot = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        var ledgerTotal = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(LedgerTotalSql, parameters, snapshot, cancellationToken: cancellationToken));
        var mismatches = await connection.QueryAsync<WalletMismatchRow>(
            new CommandDefinition(WalletMismatchesSql, parameters, snapshot, cancellationToken: cancellationToken));

        await snapshot.CommitAsync(cancellationToken);

        return new LedgerCheckResult(ledgerTotal, mismatches.Select(row => row.ToWalletMismatch()).ToList());
    }

    private sealed class WalletMismatchRow
    {
        public Guid AccountId { get; init; }

        public long SnapshotBalanceInPaise { get; init; }

        public long DerivedBalanceInPaise { get; init; }

        public long SnapshotHeldInPaise { get; init; }

        public long DerivedHeldInPaise { get; init; }

        public WalletMismatch ToWalletMismatch() => new(
            AccountId, SnapshotBalanceInPaise, DerivedBalanceInPaise, SnapshotHeldInPaise, DerivedHeldInPaise);
    }
}
