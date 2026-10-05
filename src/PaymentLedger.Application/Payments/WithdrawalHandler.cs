using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Posting;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Payments;

public sealed class WithdrawalHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    LedgerPostingService posting,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<TransactionResponse> HandleAsync(WithdrawalCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var wallets = await accounts.LockAsync([command.AccountId], cancellationToken);
        var wallet = wallets.Get(command.AccountId);
        requestContext.Requester.EnsureCanAccess(wallet);

        var withdrawal = Transaction.Withdrawal(
            wallet, new Money(command.AmountInPaise, wallet.Currency), requestContext.ToOrigin(now));
        posting.Post(withdrawal, wallets.All, now);

        transactions.Add(withdrawal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(withdrawal);
    }
}
