using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.IntegrationTests.Infrastructure;

namespace PaymentLedger.IntegrationTests.Holds;

public class HoldTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>
{
    [Fact]
    public async Task Hold_ReducesAvailableBalanceUntilCaptured()
    {
        var (customer, wallet, merchant) = await ArrangeAsync();

        var hold = await LedgerClient.ReadAsync<HoldResponse>(
            await customer.PlaceHoldAsync(wallet, merchant, 30_000, DateTimeOffset.UtcNow.AddHours(1)));

        var account = await customer.GetAccountAsync(wallet);
        account.BalanceInPaise.ShouldBe(50_000);
        account.AvailableInPaise.ShouldBe(20_000);

        var withdrawal = await customer.WithdrawAsync(wallet, 25_000);
        withdrawal.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var capture = await customer.CaptureHoldAsync(hold.Id, 10_000);

        capture.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await LedgerClient.ReadAsync<TransactionResponse>(capture)).Type.ShouldBe(TransactionType.HoldCapture);
        var afterCapture = await customer.GetAccountAsync(wallet);
        afterCapture.BalanceInPaise.ShouldBe(40_000);
        afterCapture.AvailableInPaise.ShouldBe(40_000);
        (await customer.GetHoldAsync(hold.Id)).Status.ShouldBe(HoldStatus.Captured);
    }

    [Fact]
    public async Task ExpireHolds_ReleasesFundsOfExpiredHolds()
    {
        var (customer, wallet, merchant) = await ArrangeAsync();
        var hold = await LedgerClient.ReadAsync<HoldResponse>(
            await customer.PlaceHoldAsync(wallet, merchant, 30_000, DateTimeOffset.UtcNow.AddSeconds(1)));
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExpireHoldsHandler>()
                .HandleAsync(batchSize: 1_000, TestContext.Current.CancellationToken);
        }

        (await customer.GetHoldAsync(hold.Id)).Status.ShouldBe(HoldStatus.Expired);
        (await customer.GetAccountAsync(wallet)).AvailableInPaise.ShouldBe(50_000);
    }

    private async Task<(LedgerClient Customer, Guid Wallet, Guid Merchant)> ArrangeAsync()
    {
        var admin = await LedgerClient.AdminAsync(factory);
        var customer = await LedgerClient.CustomerAsync(factory);
        var wallet = await admin.OpenWalletAsync(customer.UserId);
        var merchant = await admin.OpenWalletAsync(Guid.NewGuid());
        await customer.FundAsync(wallet.Id, 50_000);
        return (customer, wallet.Id, merchant.Id);
    }
}
