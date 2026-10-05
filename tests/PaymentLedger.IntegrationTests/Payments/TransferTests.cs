using System.Net;
using System.Net.Http.Json;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.IntegrationTests.Infrastructure;

namespace PaymentLedger.IntegrationTests.Payments;

public class TransferTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>, IAsyncLifetime
{
    private LedgerClient _admin = null!;

    public async ValueTask InitializeAsync() => _admin = await LedgerClient.AdminAsync(factory);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Transfer_MovesMoneyAndChargesTheFee()
    {
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: 100_000);

        var response = await customer.TransferAsync(sender, recipient, 25_000);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var transfer = await LedgerClient.ReadAsync<TransactionResponse>(response);
        transfer.Status.ShouldBe(TransactionStatus.Posted);
        transfer.FeeInPaise.ShouldBe(LedgerApiFactory.TransferFeeInPaise);
        transfer.Entries.ShouldContain(entry => entry.AccountId == SystemAccounts.FeeRevenueId);
        (await customer.GetAccountAsync(sender)).BalanceInPaise.ShouldBe(100_000 - 25_000 - LedgerApiFactory.TransferFeeInPaise);
    }

    [Fact]
    public async Task Transfer_WithoutEnoughFunds_IsRecordedAsFailed()
    {
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: 1_000);

        var response = await customer.TransferAsync(sender, recipient, 1_000);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await LedgerClient.ReadProblemAsync(response);
        problem.Transaction!.Status.ShouldBe(TransactionStatus.Failed);
        problem.Transaction.FailureReason.ShouldBe(TransactionFailureReason.InsufficientFunds);
        (await customer.GetAccountAsync(sender)).BalanceInPaise.ShouldBe(1_000);
    }

    [Fact]
    public async Task Transfer_RetriedWithSameKey_MovesMoneyOnce()
    {
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: 100_000);

        var first = await customer.TransferAsync(sender, recipient, 10_000, "retry-key");
        var retry = await customer.TransferAsync(sender, recipient, 10_000, "retry-key");

        retry.StatusCode.ShouldBe(HttpStatusCode.Created);
        retry.Headers.GetValues("Idempotent-Replayed").ShouldHaveSingleItem().ShouldBe("true");
        (await LedgerClient.ReadAsync<TransactionResponse>(retry)).Id
            .ShouldBe((await LedgerClient.ReadAsync<TransactionResponse>(first)).Id);
        (await _admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(10_000);
    }

    [Fact]
    public async Task Transfer_ReusingKeyForDifferentRequest_IsRejected()
    {
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: 100_000);
        await customer.TransferAsync(sender, recipient, 10_000, "reused-key");

        var response = await customer.TransferAsync(sender, recipient, 20_000, "reused-key");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await _admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(10_000);
    }

    [Fact]
    public async Task Transfer_ConcurrentRequestsWithSameKey_ExecuteOnce()
    {
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: 100_000);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => customer.TransferAsync(sender, recipient, 1_000, "same-key-in-parallel")));

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.Created);
        var ids = await Task.WhenAll(responses.Select(async response =>
            (await LedgerClient.ReadAsync<TransactionResponse>(response)).Id));
        ids.Distinct().ShouldHaveSingleItem();
        (await _admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(1_000);
    }

    [Fact]
    public async Task Transfer_ManyConcurrentRequests_NeverOverdrawTheSender()
    {
        const long startingBalance = 100_000;
        const long amount = 5_000;
        const int attempts = 30;
        var (customer, sender, recipient) = await OpenFundedPairAsync(senderPaise: startingBalance);

        var responses = await Task.WhenAll(Enumerable.Range(0, attempts)
            .Select(_ => customer.TransferAsync(sender, recipient, amount)));

        var costPerTransfer = amount + LedgerApiFactory.TransferFeeInPaise;
        var expectedSuccesses = (int)(startingBalance / costPerTransfer);
        responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(expectedSuccesses);
        responses.Count(response => response.StatusCode == HttpStatusCode.UnprocessableEntity)
            .ShouldBe(attempts - expectedSuccesses);
        (await customer.GetAccountAsync(sender)).BalanceInPaise.ShouldBe(startingBalance - (expectedSuccesses * costPerTransfer));
        (await _admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(expectedSuccesses * amount);
    }

    [Fact]
    public async Task Transfer_FromSomeoneElsesWallet_IsForbidden()
    {
        var (_, victimWallet, _) = await OpenFundedPairAsync(senderPaise: 10_000);
        var (attacker, attackerWallet, _) = await OpenFundedPairAsync(senderPaise: 0);

        var response = await attacker.TransferAsync(victimWallet, attackerWallet, 5_000);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deposit_WithoutIdempotencyKey_IsRejected()
    {
        var (customer, wallet, _) = await OpenFundedPairAsync(senderPaise: 0);

        var response = await customer.Http.PostAsJsonAsync(
            "/api/deposits",
            new { accountId = wallet, amountInPaise = 100 },
            LedgerClient.JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_WithZeroAmount_FailsValidation()
    {
        var (customer, wallet, _) = await OpenFundedPairAsync(senderPaise: 0);

        var response = await customer.DepositAsync(wallet, 0);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<(LedgerClient Customer, Guid Sender, Guid Recipient)> OpenFundedPairAsync(long senderPaise)
    {
        var customer = await LedgerClient.CustomerAsync(factory);
        var sender = await _admin.OpenWalletAsync(customer.UserId);
        var recipient = await _admin.OpenWalletAsync(Guid.NewGuid());
        if (senderPaise > 0)
        {
            await customer.FundAsync(sender.Id, senderPaise);
        }

        return (customer, sender.Id, recipient.Id);
    }
}
