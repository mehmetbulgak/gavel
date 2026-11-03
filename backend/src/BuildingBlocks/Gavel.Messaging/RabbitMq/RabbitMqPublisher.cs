using System.Diagnostics;
using System.Globalization;
using System.Text;
using Gavel.Messaging.Outbox;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Gavel.Messaging.RabbitMq;

public sealed class RabbitMqPublisher(RabbitMqConnection connection, IOptions<MessagingOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        using var activity = MessagingTelemetry.Source.StartActivity(
            $"{message.Type} publish",
            ActivityKind.Producer,
            MessagingTelemetry.ParentFrom(message.TraceParent, message.TraceState));
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", options.Value.Exchange);
        activity?.SetTag("messaging.message.id", message.Id.ToString());

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                [MessageHeaders.OccurredAt] = message.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
                [MessageHeaders.TraceParent] = activity?.Id ?? message.TraceParent,
                [MessageHeaders.TraceState] = activity?.TraceStateString ?? message.TraceState,
            },
        };

        var channel = await GetChannelAsync(ct);
        await channel.BasicPublishAsync(
            options.Value.Exchange,
            message.Type,
            mandatory: true,
            properties,
            Encoding.UTF8.GetBytes(message.Payload),
            ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _gate.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            var amqp = await connection.GetAsync(ct);
            _channel = await amqp.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                ct);
            await RabbitMqTopology.DeclareExchangeAsync(_channel, options.Value, ct);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }
}
