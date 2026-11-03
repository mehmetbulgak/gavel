using System.Collections.Concurrent;
using Gavel.Contracts;
using Gavel.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gavel.TestKit;

public sealed class EventProbe : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentQueue<(string Type, byte[] Body)> _received = new();
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private EventProbe(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<EventProbe> StartAsync(string connectionString, string exchange)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await channel.QueueDeclareAsync(string.Empty, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue.QueueName, exchange, "#");

        var probe = new EventProbe(connection, channel);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) =>
        {
            probe._received.Enqueue((delivery.BasicProperties.Type ?? string.Empty, delivery.Body.ToArray()));
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer);
        return probe;
    }

    public async Task<TEvent> WaitForAsync<TEvent>(Func<TEvent, bool> match, TimeSpan? timeout = null)
        where TEvent : IIntegrationEvent
    {
        var name = IntegrationEventNames.Of<TEvent>();
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

        while (DateTime.UtcNow < deadline)
        {
            var found = _received
                .Where(m => m.Type == name)
                .Select(m => (TEvent)MessageSerializer.Deserialize(m.Body, typeof(TEvent)))
                .FirstOrDefault(match);
            if (found is not null)
            {
                return found;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"No {name} event matched within the timeout.");
    }

    public IReadOnlyList<TEvent> Received<TEvent>()
        where TEvent : IIntegrationEvent
    {
        var name = IntegrationEventNames.Of<TEvent>();
        return _received
            .Where(m => m.Type == name)
            .Select(m => (TEvent)MessageSerializer.Deserialize(m.Body, typeof(TEvent)))
            .ToList();
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
