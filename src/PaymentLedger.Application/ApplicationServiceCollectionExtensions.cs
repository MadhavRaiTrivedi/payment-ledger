using Microsoft.Extensions.DependencyInjection;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Application.Observability;
using PaymentLedger.Application.Payments;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Application.Statements;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Domain.Posting;

namespace PaymentLedger.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<LedgerOptions>().BindConfiguration(LedgerOptions.SectionName);
        services.AddOptions<IdempotencyOptions>().BindConfiguration(IdempotencyOptions.SectionName);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LedgerPostingService>();
        services.AddSingleton<LedgerMetrics>();
        services.AddScoped<IdempotentCommandRunner>();

        services.AddScoped<OpenWalletHandler>();
        services.AddScoped<ChangeAccountStatusHandler>();
        services.AddScoped<GetAccountHandler>();
        services.AddScoped<ListAccountsHandler>();

        services.AddScoped<DepositHandler>();
        services.AddScoped<WithdrawalHandler>();
        services.AddScoped<TransferHandler>();

        services.AddScoped<GetTransactionHandler>();
        services.AddScoped<RefundHandler>();
        services.AddScoped<ReversalHandler>();

        services.AddScoped<PlaceHoldHandler>();
        services.AddScoped<CaptureHoldHandler>();
        services.AddScoped<ReleaseHoldHandler>();
        services.AddScoped<GetHoldHandler>();
        services.AddScoped<ExpireHoldsHandler>();

        services.AddScoped<GetStatementHandler>();
        services.AddScoped<GetBalanceHandler>();

        services.AddScoped<ReconcileLedgerHandler>();
        services.AddScoped<GetLatestReconciliationHandler>();

        return services;
    }
}
