using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Accounts;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Application.Holds;

public sealed class GetHoldHandler(IHoldRepository holds, IAccountRepository accounts, IRequestContext requestContext)
{
    public async Task<HoldResponse> HandleAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var hold = await holds.FindAsync(holdId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hold), holdId);
        var wallet = await accounts.FindAsync(hold.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), hold.AccountId);
        requestContext.Requester.EnsureCanAccess(wallet);

        return HoldResponse.From(hold);
    }
}
