using Gavel.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gavel.Messaging.Outbox;

internal sealed partial class OutboxPublisher<TDbContext>(
    IServiceScopeFactory scopeFactory,
    RabbitMqPublisher publisher,
    OutboxSignal signal,
    IOptions<MessagingOptions> options,
    TimeProvider clock,
    ILogger<OutboxPublisher<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext, IOutboxDbContext
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private DateTimeOffset _nextCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var polling = TimeSpan.FromMilliseconds(options.Value.OutboxPollingMilliseconds);
        var backoff = polling;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var outcome = await PublishBatchAsync(stoppingToken);
                await CleanupIfDueAsync(stoppingToken);

                if (outcome == BatchOutcome.Failed)
                {
                    await Task.Delay(backoff, clock, stoppingToken);
                    backoff = NextBackoff(backoff);
                    continue;
                }

                backoff = polling;
                if (outcome == BatchOutcome.Drained)
                {
                    await signal.WaitAsync(polling, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogPublishLoopFailed(logger, exception);
                await Task.Delay(backoff, clock, stoppingToken);
                backoff = NextBackoff(backoff);
            }
        }
    }

    private async Task<BatchOutcome> PublishBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var batchSize = options.Value.PublishBatchSize;

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var batch = await db.OutboxMessages
                .FromSql($"SELECT * FROM outbox_messages WHERE processed_at IS NULL ORDER BY occurred_at, id LIMIT {batchSize} FOR UPDATE SKIP LOCKED")
                .ToListAsync(ct);

            var outcome = batch.Count == batchSize ? BatchOutcome.Full : BatchOutcome.Drained;
            foreach (var message in batch)
            {
                try
                {
                    await publisher.PublishAsync(message, ct);
                    message.ProcessedAt = clock.GetUtcNow();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    message.Attempts++;
                    message.LastError = Truncate(exception.Message);
                    LogPublishFailed(logger, exception, message.Id, message.Type, message.Attempts);
                    outcome = BatchOutcome.Failed;
                    break;
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return outcome;
        });
    }

    private async Task CleanupIfDueAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (now < _nextCleanup)
        {
            return;
        }

        _nextCleanup = now.Add(CleanupInterval);
        var cutoff = now.AddDays(-options.Value.RetentionDays);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.OutboxMessages.Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff).ExecuteDeleteAsync(ct);
    }

    private static TimeSpan NextBackoff(TimeSpan current) => TimeSpan.FromTicks(Math.Min(current.Ticks * 2, MaxBackoff.Ticks));

    private static string Truncate(string value) =>
        value.Length <= OutboxMessage.MaxErrorLength ? value : value[..OutboxMessage.MaxErrorLength];

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing outbox message {MessageId} ({MessageType}) failed on attempt {Attempt}")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception, Guid messageId, string messageType, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "The outbox publisher loop failed; retrying")]
    private static partial void LogPublishLoopFailed(ILogger logger, Exception exception);

    private enum BatchOutcome
    {
        Drained,
        Full,
        Failed,
    }
}
