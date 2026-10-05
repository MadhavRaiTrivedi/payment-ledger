using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PaymentLedger.Application.Accounts;
using PaymentLedger.Application.Holds;
using PaymentLedger.Application.Idempotency;
using PaymentLedger.Application.Persistence;
using PaymentLedger.Application.Reconciliation;
using PaymentLedger.Application.Statements;
using PaymentLedger.Application.Transactions;
using PaymentLedger.Infrastructure.Accounts;
using PaymentLedger.Infrastructure.Holds;
using PaymentLedger.Infrastructure.Idempotency;
using PaymentLedger.Infrastructure.Messaging;
using PaymentLedger.Infrastructure.Outbox;
using PaymentLedger.Infrastructure.Persistence;
using PaymentLedger.Infrastructure.Reconciliation;
using PaymentLedger.Infrastructure.Statements;
using PaymentLedger.Infrastructure.Transactions;

namespace PaymentLedger.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(LedgerDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{LedgerDbContext.ConnectionStringName}' is not configured.");

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.AddSingleton<DomainEventsToOutboxInterceptor>();
        services.AddDbContext<LedgerDbContext>((serviceProvider, options) => options
            .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IHoldRepository, HoldRepository>();
        services.AddScoped<IReconciliationRunRepository, ReconciliationRunRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IStatementReader, StatementReader>();
        services.AddScoped<ILedgerChecks, LedgerChecks>();

        services.AddOptions<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.SectionName);
        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName);
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<RabbitMqPublisher>();
        services.AddScoped<OutboxPublisher>();
        services.AddScoped<ProcessedMessageStore>();

        return services;
    }
}
