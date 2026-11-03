using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gavel.Persistence;

public static partial class DatabaseMigrator
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static async Task MigrateDatabaseAsync<TContext>(this IHost host, CancellationToken ct = default)
        where TContext : DbContext
    {
        await using var scope = host.Services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.MigrateOnStartup)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(ct);
                LogMigrated(logger, typeof(TContext).Name);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts && exception is not OperationCanceledException)
            {
                LogRetrying(logger, exception, typeof(TContext).Name, attempt);
                await Task.Delay(RetryDelay, ct);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database for {Context} is up to date")]
    private static partial void LogMigrated(ILogger logger, string context);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Migrating the database for {Context} failed on attempt {Attempt}; retrying")]
    private static partial void LogRetrying(ILogger logger, Exception exception, string context, int attempt);
}
