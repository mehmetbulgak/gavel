using Gavel.Contracts.Auctions;
using Gavel.Messaging;
using Gavel.TestKit;
using RabbitMQ.Client;

namespace Gavel.Notifications.IntegrationTests;

[Collection(NotificationsCollection.Name)]
public sealed class DeadLetterTests(NotificationsFactory factory)
{
    [Fact]
    public async Task Unreadable_messages_are_dead_lettered_and_do_not_block_the_queue()
    {
        var inbox = new Inbox(factory);
        await factory.PublishAsync(new AuctionEndingSoon(Guid.NewGuid(), "Warm-up", DateTimeOffset.UtcNow, [inbox.UserId]));
        await inbox.WaitForAsync(p => p.Items.Count == 1);

        await TestPublisher.PublishRawAsync(
            factory.RabbitMqConnectionString,
            factory.Exchange,
            IntegrationEventNames.Of<BidderOutbid>(),
            "{ not json",
            Guid.NewGuid().ToString());
        await factory.PublishAsync(new BidderOutbid(Guid.NewGuid(), "After poison", inbox.UserId, 10m));

        var page = await inbox.WaitForAsync(p => p.Items.Count == 2);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1u, await DeadLetterCountAsync());
    }

    private async Task<uint> DeadLetterCountAsync()
    {
        var options = new MessagingOptions { Exchange = factory.Exchange, ServiceName = "notifications" };
        var connectionFactory = new ConnectionFactory { Uri = new Uri(factory.RabbitMqConnectionString) };
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var deadline = DateTime.UtcNow.AddSeconds(10);
        uint count;
        do
        {
            count = await channel.MessageCountAsync(options.DeadLetterQueueName);
            if (count > 0)
            {
                break;
            }

            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        return count;
    }
}
