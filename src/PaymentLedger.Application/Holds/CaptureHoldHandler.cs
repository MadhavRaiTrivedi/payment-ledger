using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Holds;

public sealed class CaptureHoldHandler(
    IHoldRepository holds,
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    LedgerPostingService posting,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<TransactionResponse> HandleAsync(CaptureHoldCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hold = await holds.LockAsync(command.HoldId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hold), command.HoldId);
        var wallets = await accounts.LockAsync([hold.AccountId, hold.BeneficiaryAccountId], cancellationToken);
        requestContext.Requester.EnsureCanAccess(wallets.Get(hold.AccountId));

        var capture = Transaction.HoldCapture(
            hold, new Money(command.AmountInPaise, hold.Currency), requestContext.ToOrigin(now));
        posting.Post(capture, wallets.All, now, hold);

        transactions.Add(capture);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(capture);
    }
}
