using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PaymentLedger.Application.Observability;

namespace PaymentLedger.Infrastructure.Observability;

public static class TelemetryServiceCollectionExtensions
{
    private const string OtlpEndpointSetting = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string RabbitMqActivitySources = "RabbitMQ.Client.*";

    public static OpenTelemetryBuilder AddLedgerTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddNpgsql()
                .AddHttpClientInstrumentation()
                .AddSource(RabbitMqActivitySources))
            .WithMetrics(metrics => metrics
                .AddMeter(LedgerMetrics.MeterName)
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging();

        if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointSetting]))
        {
            telemetry.UseOtlpExporter();
        }

        return telemetry;
    }
}
