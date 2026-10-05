using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Application.Holds;

public sealed class ReleaseHoldHandler(
    IHoldRepository holds,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<HoldResponse> HandleAsync(ReleaseHoldCommand command, CancellationToken cancellationToken)
    {
        var hold = await holds.LockAsync(command.HoldId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hold), command.HoldId);
        var wallet = (await accounts.LockAsync([hold.AccountId], cancellationToken)).Get(hold.AccountId);
        requestContext.Requester.EnsureCanAccess(wallet);

        hold.Release(wallet, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldResponse.From(hold);
    }
}
