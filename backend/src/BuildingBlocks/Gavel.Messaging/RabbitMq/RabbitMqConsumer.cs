using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Gavel.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gavel.Messaging.RabbitMq;

internal sealed partial class RabbitMqConsumer<TDbContext>(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnection connection,
    IntegrationEventHandlerRegistry registry,
    IOptions<MessagingOptions> options,
    TimeProvider clock,
    ILogger<RabbitMqConsumer<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext, IInboxDbContext
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxRedeliveryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var backoff = TimeSpan.FromSeconds(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var channel = await StartConsumingAsync(stoppingToken);
                LogConsuming(logger, options.Value.QueueName, registry.EventNames.Count);
                await Task.Delay(Timeout.InfiniteTimeSpan, clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogStartFailed(logger, exception, options.Value.QueueName);
                await Task.Delay(backoff, clock, stoppingToken);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }

    private async Task<IChannel> StartConsumingAsync(CancellationToken ct)
    {
        var amqp = await connection.GetAsync(ct);
        var channel = await amqp.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqTopology.DeclareQueueAsync(channel, options.Value, registry.EventNames, ct);
        await channel.BasicQosAsync(0, options.Value.PrefetchCount, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(channel, delivery, ct);
        await channel.BasicConsumeAsync(options.Value.QueueName, autoAck: false, consumer, ct);
        return channel;
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var headers = delivery.BasicProperties.Headers;
        var parent = MessagingTelemetry.ParentFrom(
            MessageHeaders.ReadString(headers, MessageHeaders.TraceParent),
            MessageHeaders.ReadString(headers, MessageHeaders.TraceState));
        var eventName = delivery.BasicProperties.Type ?? "unknown";

        using var activity = MessagingTelemetry.Source.StartActivity($"{eventName} process", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", options.Value.QueueName);
        activity?.SetTag("messaging.message.id", delivery.BasicProperties.MessageId);

        try
        {
            await ProcessAsync(delivery, eventName, ct);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
        catch (PoisonMessageException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            LogPoisonMessage(logger, exception, delivery.BasicProperties.MessageId, eventName);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            var attempt = MessageHeaders.ReadLong(headers, MessageHeaders.DeliveryCount) + 1;
            LogHandlerFailed(logger, exception, delivery.BasicProperties.MessageId, eventName, attempt);
            await Task.Delay(RedeliveryDelay(attempt), clock, ct);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, ct);
        }
    }

    private async Task ProcessAsync(BasicDeliverEventArgs delivery, string eventName, CancellationToken ct)
    {
        if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var messageId))
        {
            throw new PoisonMessageException("The message has no valid id.");
        }

        if (!registry.TryGet(eventName, out var registration))
        {
            throw new PoisonMessageException($"No handler is registered for '{eventName}'.");
        }

        object integrationEvent;
        try
        {
            integrationEvent = MessageSerializer.Deserialize(delivery.Body.Span, registration.EventType);
        }
        catch (JsonException exception)
        {
            throw new PoisonMessageException("The message body could not be read.", exception);
        }

        var occurredAt = ReadOccurredAt(delivery);
        var consumerName = options.Value.ServiceName;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var committed = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            if (await db.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumerName, ct))
            {
                return null;
            }

            var context = new MessageContext(messageId, occurredAt);
            await registration.InvokeAsync(scope.ServiceProvider, integrationEvent, context, ct);
            db.InboxMessages.Add(new InboxMessage { MessageId = messageId, Consumer = consumerName, ProcessedAt = clock.GetUtcNow() });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return context;
        });

        if (committed is not null)
        {
            await RunAfterCommitAsync(committed, eventName, ct);
        }
    }

    private async Task RunAfterCommitAsync(MessageContext context, string eventName, CancellationToken ct)
    {
        foreach (var action in context.AfterCommitActions)
        {
            try
            {
                await action(ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogAfterCommitFailed(logger, exception, context.MessageId, eventName);
            }
        }
    }

    private DateTimeOffset ReadOccurredAt(BasicDeliverEventArgs delivery) =>
        DateTimeOffset.TryParse(
            MessageHeaders.ReadString(delivery.BasicProperties.Headers, MessageHeaders.OccurredAt),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var occurredAt)
            ? occurredAt
            : clock.GetUtcNow();

    private static TimeSpan RedeliveryDelay(long attempt) =>
        TimeSpan.FromMilliseconds(Math.Min(200 * Math.Pow(2, attempt - 1), MaxRedeliveryDelay.TotalMilliseconds));

    [LoggerMessage(Level = LogLevel.Warning, Message = "A post-commit action for message {MessageId} ({EventName}) failed")]
    private static partial void LogAfterCommitFailed(ILogger logger, Exception exception, Guid messageId, string eventName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {Queue} for {EventCount} event types")]
    private static partial void LogConsuming(ILogger logger, string queue, int eventCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not start consuming {Queue}; retrying")]
    private static partial void LogStartFailed(ILogger logger, Exception exception, string queue);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} ({EventName}) cannot be processed and was dead-lettered")]
    private static partial void LogPoisonMessage(ILogger logger, Exception exception, string? messageId, string eventName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling message {MessageId} ({EventName}) failed on attempt {Attempt}")]
    private static partial void LogHandlerFailed(ILogger logger, Exception exception, string? messageId, string eventName, long attempt);
}
