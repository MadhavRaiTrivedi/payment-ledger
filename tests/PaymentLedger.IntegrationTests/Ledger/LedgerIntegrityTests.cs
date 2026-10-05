using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Application.Statements;
using PaymentLedger.Application.Transactions;
using PaymentLedger.IntegrationTests.Infrastructure;

namespace PaymentLedger.IntegrationTests.Ledger;

public class LedgerIntegrityTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>
{
    [Fact]
    public async Task Database_RejectsAnUnbalancedTransaction()
    {
        var deposit = await PostDepositAsync();

        var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteSqlAsync(
            $"""
            INSERT INTO entries (transaction_id, account_id, direction, amount_in_paise, currency, created_at)
            VALUES ('{deposit.Id}', '{deposit.DestinationAccountId}', 'Credit', 1, 'INR', now())
            """));

        exception.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Database_RejectsChangesToExistingEntries()
    {
        var deposit = await PostDepositAsync();

        var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteSqlAsync(
            $"UPDATE entries SET amount_in_paise = amount_in_paise + 1 WHERE transaction_id = '{deposit.Id}'"));

        exception.SqlState.ShouldBe(PostgresErrorCodes.RestrictViolation);
    }

    [Fact]
    public async Task Reconciliation_AfterActivity_IsBalanced()
    {
        await PostDepositAsync();
        var admin = await LedgerClient.AdminAsync(factory);

        var response = await admin.Http.PostAsync("/api/reconciliation-runs", null, TestContext.Current.CancellationToken);

        var run = await LedgerClient.ReadAsync<ReconciliationRunResponse>(response);
        run.Outcome.ShouldBe(ReconciliationOutcome.Balanced);
        run.LedgerTotalInPaise.ShouldBe(0);
    }

    [Fact]
    public async Task Reconciliation_DetectsATamperedWalletSnapshot()
    {
        var deposit = await PostDepositAsync();
        var admin = await LedgerClient.AdminAsync(factory);

        await ExecuteSqlAsync(
            $"UPDATE accounts SET balance_in_paise = balance_in_paise + 1 WHERE id = '{deposit.DestinationAccountId}'");
        try
        {
            var run = await LedgerClient.ReadAsync<ReconciliationRunResponse>(
                await admin.Http.PostAsync("/api/reconciliation-runs", null, TestContext.Current.CancellationToken));

            run.Outcome.ShouldBe(ReconciliationOutcome.MismatchFound);
            run.WalletMismatches.ShouldContain(mismatch => mismatch.AccountId == deposit.DestinationAccountId);
        }
        finally
        {
            await ExecuteSqlAsync(
                $"UPDATE accounts SET balance_in_paise = balance_in_paise - 1 WHERE id = '{deposit.DestinationAccountId}'");
        }
    }

    [Fact]
    public async Task Statement_PagesThroughEntriesWithRunningBalance()
    {
        var admin = await LedgerClient.AdminAsync(factory);
        var wallet = await admin.OpenWalletAsync(Guid.NewGuid());
        foreach (var amount in new long[] { 1_000, 2_000, 3_000 })
        {
            await admin.FundAsync(wallet.Id, amount);
        }

        var firstPage = await admin.GetAsync<StatementResponse>($"/api/accounts/{wallet.Id}/statement?limit=2");
        var secondPage = await admin.GetAsync<StatementResponse>($"/api/accounts/{wallet.Id}/statement?limit=2&after={firstPage.NextCursor}");

        firstPage.Lines.Select(line => line.BalanceAfterInPaise).ShouldBe([1_000L, 3_000L]);
        secondPage.Lines.ShouldHaveSingleItem().BalanceAfterInPaise.ShouldBe(6_000);
        secondPage.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task Balance_AsOfAPastMoment_IgnoresLaterEntries()
    {
        var admin = await LedgerClient.AdminAsync(factory);
        var wallet = await admin.OpenWalletAsync(Guid.NewGuid());
        await admin.FundAsync(wallet.Id, 1_000);
        var between = DateTimeOffset.UtcNow;
        await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        await admin.FundAsync(wallet.Id, 5_000);

        var past = await admin.GetAsync<BalanceResponse>(
            $"/api/accounts/{wallet.Id}/balance?asOf={Uri.EscapeDataString(between.ToString("O"))}");
        var current = await admin.GetAsync<BalanceResponse>($"/api/accounts/{wallet.Id}/balance");

        past.BalanceInPaise.ShouldBe(1_000);
        current.BalanceInPaise.ShouldBe(6_000);
    }

    private async Task<TransactionResponse> PostDepositAsync()
    {
        var admin = await LedgerClient.AdminAsync(factory);
        var wallet = await admin.OpenWalletAsync(Guid.NewGuid());
        return await admin.FundAsync(wallet.Id, 10_000);
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
