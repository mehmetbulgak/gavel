using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Trace;

namespace Gavel.Persistence;

public static class PersistenceExtensions
{
    private const int MaxRetryCount = 3;

    public static IHostApplicationBuilder AddGavelDbContext<TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<IServiceProvider, DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        builder.Services.AddDbContext<TContext>((services, options) =>
        {
            var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

            options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(MaxRetryCount))
                .UseSnakeCaseNamingConvention();
            configure?.Invoke(services, options);
        });

        builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);
        builder.Services.AddHealthChecks().AddDbContextCheck<TContext>("database", tags: [ServiceDefaultsExtensions.ReadyTag]);
        builder.Services.AddSingleton<IExceptionClassifier, DatabaseExceptionClassifier>();
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddNpgsql());

        return builder;
    }
}
