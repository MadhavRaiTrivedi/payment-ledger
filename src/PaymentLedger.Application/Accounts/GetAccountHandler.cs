using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;
using PaymentLedger.Application.Statements;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Accounts;

public sealed class GetAccountHandler(
    IAccountRepository accounts,
    IStatementReader statements,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<AccountResponse> HandleAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);
        requestContext.Requester.EnsureCanAccess(account);

        if (account.IsCustomerWallet)
        {
            return AccountResponse.From(account);
        }

        var derivedBalance = await statements.GetBalanceAsOfAsync(account.Id, clock.GetUtcNow(), cancellationToken);
        return AccountResponse.From(account) with
        {
            BalanceInPaise = derivedBalance,
            AvailableInPaise = derivedBalance,
        };
    }
}
