using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Transactions;

namespace PaymentLedger.Application.Transactions;

public sealed class GetTransactionHandler(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    IRequestContext requestContext)
{
    public async Task<TransactionResponse> HandleAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var transaction = await transactions.FindAsync(transactionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), transactionId);

        var requester = requestContext.Requester;
        if (!requester.IsAdmin)
        {
            var wallets = await accounts.ListByIdsAsync(transaction.CustomerWalletIds.ToList(), cancellationToken);
            if (!wallets.Any(requester.CanAccess))
            {
                throw new AccessDeniedException($"You do not have access to transaction {transactionId}.");
            }
        }

        return TransactionResponse.From(transaction);
    }
}
