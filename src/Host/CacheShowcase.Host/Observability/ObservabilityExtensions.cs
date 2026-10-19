using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CacheShowcase.Host.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Exports metrics (including <c>cache.*</c> per namespace, pattern and outcome), traces and logs over OTLP to the
    /// Aspire dashboard in docker-compose. The endpoint comes from <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(CacheMetrics.MeterName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql())
            .UseOtlpExporter();

        return builder;
    }
}
