using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public sealed class RefundHandler(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    LedgerPostingService posting,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<TransactionResponse> HandleAsync(RefundCommand command, CancellationToken cancellationToken)
    {
        requestContext.Requester.EnsureAdmin();
        var now = clock.GetUtcNow();

        // Locking the original stops two concurrent refunds from both passing the remaining-amount check.
        var original = await transactions.LockAsync(command.TransactionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), command.TransactionId);

        var refund = Transaction.Refund(
            original, new Money(command.AmountInPaise, original.Currency), requestContext.ToOrigin(now));
        var wallets = await accounts.LockAsync(refund.CustomerWalletIds.ToList(), cancellationToken);

        posting.Post(refund, wallets.All, now);
        if (refund.Status == TransactionStatus.Posted)
        {
            original.RecordRefund(refund.Amount);
        }

        transactions.Add(refund);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(refund);
    }
}
