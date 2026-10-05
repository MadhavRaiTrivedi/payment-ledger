using PaymentLedger.Application.Security;

namespace PaymentLedger.Application.Accounts;

public sealed class ListAccountsHandler(IAccountRepository accounts, IRequestContext requestContext)
{
    public async Task<IReadOnlyList<AccountResponse>> HandleAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        var requester = requestContext.Requester;
        var effectiveOwnerId = ownerId ?? requester.UserId;
        if (effectiveOwnerId != requester.UserId)
        {
            requester.EnsureAdmin();
        }

        var owned = await accounts.ListByOwnerAsync(effectiveOwnerId, cancellationToken);
        return owned.Select(AccountResponse.From).ToList();
    }
}
