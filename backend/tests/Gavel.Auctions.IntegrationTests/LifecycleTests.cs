using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Application.Me;
using Gavel.Auctions.Domain;
using Gavel.Contracts.Auctions;
using Gavel.TestKit;
using ContractOutcome = Gavel.Contracts.Auctions.AuctionOutcome;

namespace Gavel.Auctions.IntegrationTests;

[Collection(AuctionsCollection.Name)]
public sealed class LifecycleTests(AuctionsFactory factory) : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => factory.Clock.Reset();

    [Fact]
    public async Task Ending_an_auction_sells_to_the_leader_and_tells_everyone()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var bob = new Member(factory, "Bob");
        var auction = await seller.CreateAuctionAsync(title: "Mechanical watch", durationMinutes: 5);
        await alice.BidOkAsync(auction.Id, 300m);
        await bob.BidOkAsync(auction.Id, 250m);

        factory.Clock.Advance(TimeSpan.FromMinutes(6));
        await factory.RunLifecycleAsync();

        var ended = await probe.WaitForAsync<AuctionEnded>(e => e.AuctionId == auction.Id);
        Assert.Equal(ContractOutcome.Sold, ended.Outcome);
        Assert.Equal(alice.Id, ended.WinnerId);
        Assert.Equal(260m, ended.FinalPrice);
        Assert.Equal(seller.Id, ended.SellerId);
        Assert.Equal([bob.Id], ended.LosingBidderIds);

        var details = await alice.GetAuctionAsync(auction.Id);
        Assert.Equal(AuctionStatus.Ended, details.Status);
        Assert.True(details.Viewer!.IsWinner);
        Assert.Single((await alice.GetAsync<PagedResult<MyBidDto>>("/api/me/bids?state=Won")).Items, b => b.Auction.Id == auction.Id);
        Assert.Single((await bob.GetAsync<PagedResult<MyBidDto>>("/api/me/bids?state=Lost")).Items, b => b.Auction.Id == auction.Id);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, (await bob.BidAsync(auction.Id, 9_000m)).StatusCode);
    }

    [Fact]
    public async Task Auctions_below_the_reserve_end_unsold()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);
        var seller = new Member(factory, "Seller");
        var bidder = new Member(factory, "Bidder");
        var auction = await seller.CreateAuctionAsync(startingPrice: 100m, reservePrice: 1_000m, durationMinutes: 5);
        await bidder.BidOkAsync(auction.Id, 500m);

        factory.Clock.Advance(TimeSpan.FromMinutes(6));
        await factory.RunLifecycleAsync();

        var ended = await probe.WaitForAsync<AuctionEnded>(e => e.AuctionId == auction.Id);
        Assert.Equal(ContractOutcome.ReserveNotMet, ended.Outcome);
        Assert.Null(ended.WinnerId);
        Assert.Equal([bidder.Id], ended.LosingBidderIds);
    }

    [Fact]
    public async Task Watchers_and_bidders_hear_when_an_auction_is_ending_soon()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);
        var seller = new Member(factory, "Seller");
        var watcher = new Member(factory, "Watcher");
        var bidder = new Member(factory, "Bidder");
        var auction = await seller.CreateAuctionAsync(durationMinutes: 15);
        await watcher.Client.PutAsync($"/api/auctions/{auction.Id}/watch", null, Ct);
        await bidder.BidOkAsync(auction.Id, 150m);

        factory.Clock.Advance(TimeSpan.FromMinutes(11));
        await factory.RunLifecycleAsync();
        await factory.RunLifecycleAsync();

        var soon = await probe.WaitForAsync<AuctionEndingSoon>(e => e.AuctionId == auction.Id);
        Assert.Equal(new[] { bidder.Id, watcher.Id }.Order(), soon.RecipientIds.Order());
        await Task.Delay(500, Ct);
        Assert.Single(probe.Received<AuctionEndingSoon>(), e => e.AuctionId == auction.Id);
    }
}
