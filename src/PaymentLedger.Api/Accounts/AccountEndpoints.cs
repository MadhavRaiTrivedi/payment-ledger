using PaymentLedger.Api.Security;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Statements;

namespace PaymentLedger.Api.Accounts;

internal static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/accounts").WithTags("Accounts").RequireAuthorization();

        accounts.MapPost("/", OpenWalletAsync).RequireAuthorization(AuthorizationPolicies.Admin);
        accounts.MapGet("/", ListAsync);
        accounts.MapGet("/{accountId:guid}", GetAsync);
        accounts.MapGet("/{accountId:guid}/balance", GetBalanceAsync);
        accounts.MapGet("/{accountId:guid}/statement", GetStatementAsync);
        MapStatusChange(accounts, "/{accountId:guid}/freeze", AccountStatusChange.Freeze);
        MapStatusChange(accounts, "/{accountId:guid}/unfreeze", AccountStatusChange.Unfreeze);
        MapStatusChange(accounts, "/{accountId:guid}/close", AccountStatusChange.Close);
    }

    private static void MapStatusChange(RouteGroupBuilder accounts, string pattern, AccountStatusChange change) =>
        accounts.MapPost(pattern, (Guid accountId, ChangeAccountStatusHandler handler, CancellationToken cancellationToken) =>
                handler.HandleAsync(new ChangeAccountStatusCommand(accountId, change), cancellationToken))
            .RequireAuthorization(AuthorizationPolicies.Admin);

    private static async Task<IResult> OpenWalletAsync(
        OpenWalletRequest request,
        OpenWalletHandler handler,
        CancellationToken cancellationToken)
    {
        var wallet = await handler.HandleAsync(new OpenWalletCommand(request.OwnerId, request.Currency), cancellationToken);
        return TypedResults.Created($"/api/accounts/{wallet.Id}", wallet);
    }

    private static Task<IReadOnlyList<AccountResponse>> ListAsync(
        Guid? ownerId,
        ListAccountsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(ownerId, cancellationToken);

    private static Task<AccountResponse> GetAsync(
        Guid accountId,
        GetAccountHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(accountId, cancellationToken);

    private static Task<BalanceResponse> GetBalanceAsync(
        Guid accountId,
        DateTimeOffset? asOf,
        GetBalanceHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(accountId, asOf, cancellationToken);

    private static Task<StatementResponse> GetStatementAsync(
        Guid accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        long? after,
        int? limit,
        GetStatementHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new StatementQuery(accountId, from, to, after, limit), cancellationToken);
}
