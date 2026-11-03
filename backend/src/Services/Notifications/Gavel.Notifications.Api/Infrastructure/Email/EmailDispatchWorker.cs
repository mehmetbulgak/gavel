using Microsoft.Extensions.Options;

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed partial class EmailDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<EmailDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || !options.Value.DispatchEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.DispatchIntervalSeconds), clock);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<EmailDispatchProcessor>().RunOnceAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    LogFailed(logger, exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The email dispatch run failed; retrying on the next tick")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
