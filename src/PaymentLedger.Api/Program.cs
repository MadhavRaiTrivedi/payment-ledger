using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PaymentLedger.Api.Accounts;
using PaymentLedger.Api.Health;
using PaymentLedger.Api.Holds;
using PaymentLedger.Api.Http;
using PaymentLedger.Api.OpenApi;
using PaymentLedger.Api.Payments;
using PaymentLedger.Api.Reconciliation;
using PaymentLedger.Api.Security;
using PaymentLedger.Api.Transactions;
using PaymentLedger.Application;
using PaymentLedger.Application.Security;
using PaymentLedger.Infrastructure;
using PaymentLedger.Infrastructure.Observability;
using PaymentLedger.Infrastructure.Persistence;
using Scalar.AspNetCore;

const string ServiceName = "payment-ledger-api";
const string ApplyMigrationsSetting = "Database:ApplyMigrationsOnStartup";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLedgerTelemetry(builder.Configuration, ServiceName)
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "Jwt:SigningKey must be at least 32 bytes.")
    .ValidateOnStart();
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = LedgerClaimTypes.UserId,
            RoleClaimType = LedgerClaimTypes.Role,
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin)));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<LedgerExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddLedgerTransformers());
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: [HealthEndpoints.ReadinessTag]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>(ApplyMigrationsSetting))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<LedgerDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthEndpoints();

if (jwt.EnableDevTokenEndpoint)
{
    app.MapDevTokenEndpoints();
}

app.MapAccountEndpoints();
app.MapPaymentEndpoints();
app.MapTransactionEndpoints();
app.MapHoldEndpoints();
app.MapReconciliationEndpoints();

await app.RunAsync();

public partial class Program;
