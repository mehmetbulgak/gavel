using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Gavel.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health/ready";
    public const string ReadyTag = "ready";

    private static readonly string[] TracingSources = ["Gavel.*", "Microsoft.AspNetCore.SignalR.Server"];

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = check => check.Name == "self" })
            .AllowAnonymous()
            .DisableRateLimiting();
        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Gavel.*"))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(TracingSources)
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }
}
