using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Accounts;

public sealed class OpenWalletHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<AccountResponse> HandleAsync(OpenWalletCommand command, CancellationToken cancellationToken)
    {
        requestContext.Requester.EnsureAdmin();

        var wallet = Account.OpenWallet(command.OwnerId, command.Currency, clock.GetUtcNow());
        accounts.Add(wallet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AccountResponse.From(wallet);
    }
}
