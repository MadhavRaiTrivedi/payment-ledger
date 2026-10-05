using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Errors;
using PaymentLedger.Application.Security;
using PaymentLedger.Domain.Accounts;

namespace PaymentLedger.Application.Statements;

public sealed class GetStatementHandler(
    IAccountRepository accounts,
    IStatementReader statements,
    IRequestContext requestContext)
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public async Task<StatementResponse> HandleAsync(StatementQuery query, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(query.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), query.AccountId);
        requestContext.Requester.EnsureCanAccess(account);

        var pageSize = Math.Clamp(query.Limit ?? DefaultPageSize, 1, MaxPageSize);

        // One extra row tells us whether there is a next page without a separate count query.
        var lines = await statements.ReadLinesAsync(
            account.Id, query.From, query.To, query.AfterSequence, pageSize + 1, cancellationToken);

        var hasMore = lines.Count > pageSize;
        var page = hasMore ? lines.Take(pageSize).ToList() : lines;
        return new StatementResponse(account.Id, page, hasMore ? page[^1].Sequence : null);
    }
}
