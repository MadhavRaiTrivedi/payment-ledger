using PaymentLedger.Application;
using PaymentLedger.Application.Security;
using PaymentLedger.Infrastructure;
using PaymentLedger.Infrastructure.Observability;
using PaymentLedger.Worker;
using PaymentLedger.Worker.Consumers;
using PaymentLedger.Worker.Jobs;

const string ServiceName = "payment-ledger-worker";

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLedgerTelemetry(builder.Configuration, ServiceName);
builder.Services.AddSingleton<IRequestContext, SystemRequestContext>();

builder.Services.AddOptions<JobScheduleOptions>().BindConfiguration(JobScheduleOptions.SectionName);
builder.Services.AddHostedService<OutboxPublishingJob>();
builder.Services.AddHostedService<HoldExpiryJob>();
builder.Services.AddHostedService<ReconciliationJob>();
builder.Services.AddHostedService<IdempotencyCleanupJob>();
builder.Services.AddHostedService<TransactionNotificationConsumer>();

await builder.Build().RunAsync();
