using Microsoft.Extensions.Options;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Payments;

public sealed class TransferHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    LedgerPostingService posting,
    IRequestContext requestContext,
    IOptions<LedgerOptions> options,
    TimeProvider clock)
{
    public async Task<TransactionResponse> HandleAsync(TransferCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var wallets = await accounts.LockAsync([command.SourceAccountId, command.DestinationAccountId], cancellationToken);
        var sender = wallets.Get(command.SourceAccountId);
        var recipient = wallets.Get(command.DestinationAccountId);
        requestContext.Requester.EnsureCanAccess(sender);

        var transfer = Transaction.Transfer(
            sender,
            recipient,
            new Money(command.AmountInPaise, sender.Currency),
            new Money(options.Value.TransferFeeInPaise, sender.Currency),
            requestContext.ToOrigin(now));
        posting.Post(transfer, wallets.All, now);

        transactions.Add(transfer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(transfer);
    }
}
