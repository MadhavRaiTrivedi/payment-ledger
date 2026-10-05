using Microsoft.Extensions.Options;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Currencies;
using PaymentLedger.Domain.Holds;

namespace PaymentLedger.Application.Holds;

public sealed class PlaceHoldHandler(
    IAccountRepository accounts,
    IHoldRepository holds,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IOptions<LedgerOptions> options,
    TimeProvider clock)
{
    public async Task<HoldResponse> HandleAsync(PlaceHoldCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var maxHoldDuration = options.Value.MaxHoldDuration;
        if (command.ExpiresAt - now > maxHoldDuration)
        {
            throw new InvalidRequestException($"A hold can last at most {maxHoldDuration}.");
        }

        var wallets = await accounts.LockAsync([command.AccountId, command.BeneficiaryAccountId], cancellationToken);
        var wallet = wallets.Get(command.AccountId);
        requestContext.Requester.EnsureCanAccess(wallet);

        var hold = Hold.Place(
            wallet,
            wallets.Get(command.BeneficiaryAccountId),
            new Money(command.AmountInPaise, wallet.Currency),
            command.ExpiresAt,
            now);

        holds.Add(hold);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldResponse.From(hold);
    }
}
