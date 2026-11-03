using Gavel.Messaging;
using RabbitMQ.Client;

namespace Gavel.TestKit;

public static class RabbitMqTestTopology
{
    public static async Task DeleteAsync(string connectionString, string exchange, string serviceName)
    {
        var options = new MessagingOptions { Exchange = exchange, ServiceName = serviceName };
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(options.QueueName, ifUnused: false, ifEmpty: false);
        await channel.QueueDeleteAsync(options.DeadLetterQueueName, ifUnused: false, ifEmpty: false);
        await channel.ExchangeDeleteAsync(options.DeadLetterExchange, ifUnused: false);
        await channel.ExchangeDeleteAsync(options.Exchange, ifUnused: false);
    }
}
