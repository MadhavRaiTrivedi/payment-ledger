using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public sealed class ReversalHandler(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    LedgerPostingService posting,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<TransactionResponse> HandleAsync(ReversalCommand command, CancellationToken cancellationToken)
    {
        requestContext.Requester.EnsureAdmin();
        var now = clock.GetUtcNow();

        var original = await transactions.LockAsync(command.TransactionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), command.TransactionId);

        var reversal = Transaction.Reversal(original, requestContext.ToOrigin(now));
        var wallets = await accounts.LockAsync(reversal.CustomerWalletIds.ToList(), cancellationToken);

        posting.Post(reversal, wallets.All, now);
        if (reversal.Status == TransactionStatus.Posted)
        {
            original.MarkReversed(reversal.Id, now);
        }

        transactions.Add(reversal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(reversal);
    }
}
