using Gavel.Auctions.Application.Lifecycle;
using Microsoft.Extensions.Options;

namespace Gavel.Auctions.Api.Lifecycle;

internal sealed partial class AuctionLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LifecycleOptions> options,
    TimeProvider clock,
    ILogger<AuctionLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.IntervalMilliseconds), clock);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processed = await scope.ServiceProvider.GetRequiredService<AuctionLifecycleProcessor>().RunOnceAsync(ct);
            if (processed > 0)
            {
                LogProcessed(logger, processed);
            }
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            LogFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Auction lifecycle processed {Count} auctions")]
    private static partial void LogProcessed(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The auction lifecycle run failed; retrying on the next tick")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
