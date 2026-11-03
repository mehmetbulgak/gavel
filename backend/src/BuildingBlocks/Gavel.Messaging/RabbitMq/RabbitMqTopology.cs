using RabbitMQ.Client;

namespace Gavel.Messaging.RabbitMq;

internal static class RabbitMqTopology
{
    public static Task DeclareExchangeAsync(IChannel channel, MessagingOptions options, CancellationToken ct) =>
        channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);

    public static async Task DeclareQueueAsync(IChannel channel, MessagingOptions options, IEnumerable<string> routingKeys, CancellationToken ct)
    {
        await DeclareExchangeAsync(channel, options, ct);
        await channel.ExchangeDeclareAsync(options.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: ct);
        await channel.QueueBindAsync(options.DeadLetterQueueName, options.DeadLetterExchange, options.QueueName, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = options.MaxDeliveryAttempts,
                ["x-dead-letter-exchange"] = options.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = options.QueueName,
            },
            cancellationToken: ct);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(options.QueueName, options.Exchange, routingKey, cancellationToken: ct);
        }
    }
}
