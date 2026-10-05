using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Statements;

public sealed class GetBalanceHandler(
    IAccountRepository accounts,
    IStatementReader statements,
    IRequestContext requestContext,
    TimeProvider clock)
{
    public async Task<BalanceResponse> HandleAsync(Guid accountId, DateTimeOffset? asOf, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);
        requestContext.Requester.EnsureCanAccess(account);

        var moment = asOf ?? clock.GetUtcNow();
        var balance = await statements.GetBalanceAsOfAsync(account.Id, moment, cancellationToken);

        return new BalanceResponse(account.Id, moment, balance, account.Currency);
    }
}
