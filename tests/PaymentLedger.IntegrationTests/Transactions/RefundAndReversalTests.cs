using System.Net;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain;
using PaymentLedger.Domain.Transactions;
using PaymentLedger.IntegrationTests.Infrastructure;

namespace PaymentLedger.IntegrationTests.Transactions;

public class RefundAndReversalTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>
{
    [Fact]
    public async Task Refund_Partial_ReturnsMoneyToSenderAndTracksRefundedAmount()
    {
        var (admin, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);

        var response = await admin.RefundAsync(transfer.Id, 15_000);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var refund = await LedgerClient.ReadAsync<TransactionResponse>(response);
        refund.Type.ShouldBe(TransactionType.Refund);
        refund.OriginalTransactionId.ShouldBe(transfer.Id);
        (await admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(25_000);
        var original = await admin.GetTransactionAsync(transfer.Id);
        original.RefundedInPaise.ShouldBe(15_000);
    }

    [Fact]
    public async Task Refund_BeyondRemainingAmount_IsRejected()
    {
        var (admin, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);
        await admin.RefundAsync(transfer.Id, 30_000);

        var response = await admin.RefundAsync(transfer.Id, 10_001);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await LedgerClient.ReadProblemAsync(response);
        problem.Code.ShouldBe(nameof(DomainErrorCode.RefundExceedsOriginal));
    }

    [Fact]
    public async Task Refund_ByCustomer_IsForbidden()
    {
        var (_, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);

        var response = await customer.RefundAsync(transfer.Id, 1_000);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reversal_UndoesEveryEntryIncludingTheFee()
    {
        var (admin, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);

        var response = await admin.ReverseAsync(transfer.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await admin.GetAccountAsync(sender)).BalanceInPaise.ShouldBe(100_000);
        (await admin.GetAccountAsync(recipient)).BalanceInPaise.ShouldBe(0);
        var original = await admin.GetTransactionAsync(transfer.Id);
        original.Status.ShouldBe(TransactionStatus.Reversed);
    }

    [Fact]
    public async Task Reversal_Twice_IsRejected()
    {
        var (admin, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);
        await admin.ReverseAsync(transfer.Id);

        var response = await admin.ReverseAsync(transfer.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Reversal_WhenRecipientAlreadySpentTheMoney_FailsAndKeepsOriginalPosted()
    {
        var (admin, customer, sender, recipient) = await ArrangeAsync();
        var transfer = await TransferAsync(customer, sender, recipient, 40_000);
        await admin.WithdrawAsync(recipient, 40_000);

        var response = await admin.ReverseAsync(transfer.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var original = await admin.GetTransactionAsync(transfer.Id);
        original.Status.ShouldBe(TransactionStatus.Posted);
    }

    private async Task<(LedgerClient Admin, LedgerClient Customer, Guid Sender, Guid Recipient)> ArrangeAsync()
    {
        var admin = await LedgerClient.AdminAsync(factory);
        var customer = await LedgerClient.CustomerAsync(factory);
        var sender = await admin.OpenWalletAsync(customer.UserId);
        var recipient = await admin.OpenWalletAsync(Guid.NewGuid());
        await customer.FundAsync(sender.Id, 100_000);
        return (admin, customer, sender.Id, recipient.Id);
    }

    private static async Task<TransactionResponse> TransferAsync(LedgerClient customer, Guid sender, Guid recipient, long amount) =>
        await LedgerClient.ReadAsync<TransactionResponse>(await customer.TransferAsync(sender, recipient, amount));
}
