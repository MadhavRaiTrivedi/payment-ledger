using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;

namespace PaymentLedger.Application.Accounts;

public sealed class ChangeAccountStatusHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext)
{
    public async Task<AccountResponse> HandleAsync(ChangeAccountStatusCommand command, CancellationToken cancellationToken)
    {
        requestContext.Requester.EnsureAdmin();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var account = (await accounts.LockAsync([command.AccountId], cancellationToken)).Get(command.AccountId);

        switch (command.Change)
        {
            case AccountStatusChange.Freeze:
                account.Freeze();
                break;
            case AccountStatusChange.Unfreeze:
                account.Unfreeze();
                break;
            case AccountStatusChange.Close:
                account.Close();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.Change, "Unknown status change.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AccountResponse.From(account);
    }
}
