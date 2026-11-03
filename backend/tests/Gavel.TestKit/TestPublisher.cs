using System.Globalization;
using System.Text;
using Gavel.Contracts;
using Gavel.Messaging;
using RabbitMQ.Client;

namespace Gavel.TestKit;

public static class TestPublisher
{
    private static readonly TimeSpan RouteTimeout = TimeSpan.FromSeconds(15);

    public static async Task<Guid> PublishWhenRoutableAsync<TEvent>(string connectionString, string exchange, TEvent integrationEvent, Guid? messageId = null)
        where TEvent : IIntegrationEvent
    {
        var deadline = DateTime.UtcNow + RouteTimeout;
        while (true)
        {
            try
            {
                return await PublishAsync(connectionString, exchange, integrationEvent, messageId);
            }
            catch (RabbitMQ.Client.Exceptions.PublishException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }
        }
    }

    public static async Task<Guid> PublishAsync<TEvent>(string connectionString, string exchange, TEvent integrationEvent, Guid? messageId = null)
        where TEvent : IIntegrationEvent
    {
        var id = messageId ?? Guid.CreateVersion7();
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true));
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var name = IntegrationEventNames.Of<TEvent>();
        var properties = new BasicProperties
        {
            MessageId = id.ToString(),
            Type = name,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                ["occurred-at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            },
        };

        await channel.BasicPublishAsync(
            exchange,
            name,
            mandatory: true,
            properties,
            Encoding.UTF8.GetBytes(MessageSerializer.Serialize(integrationEvent, typeof(TEvent))));
        return id;
    }

    public static async Task PublishRawAsync(string connectionString, string exchange, string type, string body, string? messageId)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true));
        await channel.BasicPublishAsync(
            exchange,
            type,
            mandatory: true,
            new BasicProperties { MessageId = messageId, Type = type, ContentType = "application/json" },
            Encoding.UTF8.GetBytes(body));
    }
}
