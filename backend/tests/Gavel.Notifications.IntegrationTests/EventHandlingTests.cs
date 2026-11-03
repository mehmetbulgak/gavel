using System.Net;
using Gavel.Contracts.Auctions;
using Gavel.Contracts.Identity;
using Gavel.Notifications.Api.Domain;

namespace Gavel.Notifications.IntegrationTests;

[Collection(NotificationsCollection.Name)]
public sealed class EventHandlingTests(NotificationsFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task New_users_get_a_welcome_notification()
    {
        var inbox = new Inbox(factory);

        await factory.PublishAsync(new UserRegistered(inbox.UserId, $"{inbox.UserId:N}@test.dev", "Deniz"));

        var page = await inbox.WaitForAsync(p => p.Items.Count > 0);
        var welcome = Assert.Single(page.Items);
        Assert.Equal(NotificationType.Welcome, welcome.Type);
        Assert.Equal("Welcome to Gavel, Deniz", welcome.Title);
        Assert.Equal(1, page.UnreadCount);
    }

    [Fact]
    public async Task Outbid_notifications_can_be_read_one_by_one_or_all_at_once()
    {
        var inbox = new Inbox(factory);
        var auctionId = Guid.NewGuid();

        await factory.PublishAsync(new BidderOutbid(auctionId, "Film camera", inbox.UserId, 1250m));
        await factory.PublishAsync(new BidderOutbid(auctionId, "Film camera", inbox.UserId, 1300m));
        var page = await inbox.WaitForAsync(p => p.Items.Count == 2);

        Assert.All(page.Items, n => Assert.Equal(NotificationType.Outbid, n.Type));
        Assert.Contains(page.Items, n => n.Body.Contains("₺1.300", StringComparison.Ordinal));
        Assert.Equal(2, page.UnreadCount);

        Assert.Equal(HttpStatusCode.NoContent, (await inbox.Client.PostAsync($"/api/notifications/{page.Items[0].Id}/read", null, Ct)).StatusCode);
        Assert.Equal(1, (await inbox.ListAsync()).UnreadCount);
        Assert.Equal(HttpStatusCode.NoContent, (await inbox.Client.PostAsync("/api/notifications/read-all", null, Ct)).StatusCode);
        Assert.Equal(0, (await inbox.ListAsync()).UnreadCount);
    }

    [Fact]
    public async Task Notification_counts_and_items_share_a_snapshot_during_delivery()
    {
        const int count = 12;
        var inbox = new Inbox(factory);
        var publishing = PublishAsync();
        var deadline = DateTime.UtcNow.AddSeconds(15);

        try
        {
            while (true)
            {
                var page = await inbox.ListAsync();
                Assert.Equal(page.TotalCount, page.UnreadCount);
                Assert.Equal(page.TotalCount, page.Items.Count);
                if (page.TotalCount == count)
                {
                    break;
                }

                Assert.True(DateTime.UtcNow < deadline, "All notifications should arrive before the deadline.");
                await Task.Delay(10, Ct);
            }
        }
        finally
        {
            await publishing;
        }

        async Task PublishAsync()
        {
            for (var i = 0; i < count; i++)
            {
                await factory.PublishAsync(new BidderOutbid(Guid.NewGuid(), "Concurrent delivery", inbox.UserId, 100m + i));
            }
        }
    }

    [Fact]
    public async Task A_sale_notifies_the_seller_the_winner_and_the_losers()
    {
        var seller = new Inbox(factory);
        var winner = new Inbox(factory);
        var loser = new Inbox(factory);

        await factory.PublishAsync(new AuctionEnded(Guid.NewGuid(), "Desk lamp", seller.UserId, AuctionOutcome.Sold, winner.UserId, 480m, [loser.UserId]));

        Assert.Equal(NotificationType.AuctionSold, Assert.Single((await seller.WaitForAsync(p => p.Items.Count > 0)).Items).Type);
        Assert.Equal(NotificationType.AuctionWon, Assert.Single((await winner.WaitForAsync(p => p.Items.Count > 0)).Items).Type);
        Assert.Equal(NotificationType.AuctionLost, Assert.Single((await loser.WaitForAsync(p => p.Items.Count > 0)).Items).Type);
    }

    [Fact]
    public async Task An_unsold_auction_only_notifies_the_seller()
    {
        var seller = new Inbox(factory);

        await factory.PublishAsync(new AuctionEnded(Guid.NewGuid(), "Old radio", seller.UserId, AuctionOutcome.ReserveNotMet, null, null, []));

        var notification = Assert.Single((await seller.WaitForAsync(p => p.Items.Count > 0)).Items);
        Assert.Equal(NotificationType.AuctionUnsold, notification.Type);
        Assert.Contains("reserve", notification.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Redelivered_messages_are_processed_once()
    {
        var inbox = new Inbox(factory);
        var messageId = Guid.CreateVersion7();
        var outbid = new BidderOutbid(Guid.NewGuid(), "Bike", inbox.UserId, 300m);

        await factory.PublishAsync(outbid, messageId);
        await factory.PublishAsync(outbid, messageId);
        await factory.PublishAsync(new AuctionEndingSoon(Guid.NewGuid(), "Marker", DateTimeOffset.UtcNow, [inbox.UserId]));

        var page = await inbox.WaitForAsync(p => p.Items.Any(n => n.Type == NotificationType.AuctionEndingSoon));
        Assert.Single(page.Items, n => n.Type == NotificationType.Outbid);
    }

    [Fact]
    public async Task Users_only_see_and_change_their_own_notifications()
    {
        var owner = new Inbox(factory);
        var stranger = new Inbox(factory);
        await factory.PublishAsync(new BidderOutbid(Guid.NewGuid(), "Guitar", owner.UserId, 900m));
        var notification = Assert.Single((await owner.WaitForAsync(p => p.Items.Count > 0)).Items);

        var steal = await stranger.Client.PostAsync($"/api/notifications/{notification.Id}/read", null, Ct);
        var anonymous = await factory.CreateClient().GetAsync("/api/notifications", Ct);

        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Empty((await stranger.ListAsync()).Items);
        Assert.Equal(1, (await owner.ListAsync()).UnreadCount);
    }
}
