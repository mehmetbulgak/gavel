using System.Net;
using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Domain;
using Gavel.Contracts.Auctions;
using Gavel.TestKit;

namespace Gavel.Auctions.IntegrationTests;

[Collection(AuctionsCollection.Name)]
public sealed class BiddingTests(AuctionsFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Proxy_bidding_through_the_api_keeps_the_highest_maximum_in_front()
    {
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var bob = new Member(factory, "Bob");
        var auction = await seller.CreateAuctionAsync(startingPrice: 100m);

        var first = await alice.BidOkAsync(auction.Id, 500m);
        var second = await bob.BidOkAsync(auction.Id, 300m);

        Assert.True(first.IsLeading);
        Assert.Equal("Bidder 1", first.Alias);
        Assert.False(second.IsLeading);
        Assert.Equal(310m, second.CurrentPrice);

        var asAlice = await alice.GetAuctionAsync(auction.Id);
        Assert.True(asAlice.Viewer!.IsLeading);
        Assert.Equal(500m, asAlice.Viewer.MaxBid);
        Assert.Equal("Bidder 1", asAlice.LeaderAlias);
        Assert.Equal(2, asAlice.BidderCount);
        Assert.Equal(320m, asAlice.MinimumNextBid);
    }

    [Fact]
    public async Task Being_outbid_publishes_an_event_for_the_previous_leader()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var bob = new Member(factory, "Bob");
        var auction = await seller.CreateAuctionAsync(title: "Signed vinyl record", startingPrice: 50m);

        await alice.BidOkAsync(auction.Id, 80m);
        await bob.BidOkAsync(auction.Id, 200m);

        var outbid = await probe.WaitForAsync<BidderOutbid>(e => e.AuctionId == auction.Id);
        Assert.Equal(alice.Id, outbid.UserId);
        Assert.Equal("Signed vinyl record", outbid.AuctionTitle);
        Assert.Equal(85m, outbid.CurrentPrice);
    }

    [Fact]
    public async Task Bid_history_never_reveals_who_the_bidders_are()
    {
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var bob = new Member(factory, "Bob");
        var auction = await seller.CreateAuctionAsync();
        await alice.BidOkAsync(auction.Id, 400m);
        await bob.BidOkAsync(auction.Id, 450m);

        var anonymousJson = await factory.CreateClient().GetStringAsync($"/api/auctions/{auction.Id}/bids", Ct);
        var detailsJson = await factory.CreateClient().GetStringAsync($"/api/auctions/{auction.Id}", Ct);
        var asBob = await bob.GetAsync<PagedResult<BidDto>>($"/api/auctions/{auction.Id}/bids");

        foreach (var json in new[] { anonymousJson, detailsJson })
        {
            Assert.DoesNotContain(alice.Id.ToString(), json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(bob.Id.ToString(), json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Alice", json, StringComparison.Ordinal);
            Assert.DoesNotContain("450", json, StringComparison.Ordinal);
        }

        Assert.Contains(asBob.Items, b => b.IsMine && b.Alias == "Bidder 2");
        Assert.DoesNotContain(asBob.Items, b => b.IsMine && b.Alias == "Bidder 1");
    }

    [Fact]
    public async Task Invalid_bids_are_rejected_with_a_reason()
    {
        var seller = new Member(factory, "Seller");
        var bidder = new Member(factory, "Bidder");
        var auction = await seller.CreateAuctionAsync(startingPrice: 100m);

        var own = await seller.BidAsync(auction.Id, 500m);
        var low = await bidder.BidAsync(auction.Id, 99m);
        var precise = await bidder.BidAsync(auction.Id, 150.555m);
        var missing = await bidder.BidAsync(Guid.NewGuid(), 500m);
        var anonymous = await factory.CreateClient().PostAsync($"/api/auctions/{auction.Id}/bids", null, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, own.StatusCode);
        Assert.Equal("Auction.SellerCannotBid", await own.ProblemCodeAsync());
        Assert.Equal(HttpStatusCode.BadRequest, low.StatusCode);
        Assert.Equal("Bid.TooLow", await low.ProblemCodeAsync());
        Assert.Equal(HttpStatusCode.BadRequest, precise.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Concurrent_bids_on_one_auction_are_applied_one_at_a_time()
    {
        var seller = new Member(factory, "Seller");
        var auction = await seller.CreateAuctionAsync(startingPrice: 10m);
        var bidders = Enumerable.Range(1, 24).Select(i => new Member(factory, $"Bidder {i}")).ToList();

        var responses = await Task.WhenAll(bidders.Select((b, i) => b.BidAsync(auction.Id, 1_000m + (i * 37m))));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest));
        var details = await seller.GetAuctionAsync(auction.Id);
        var history = await seller.GetAsync<PagedResult<BidDto>>($"/api/auctions/{auction.Id}/bids?pageSize=100");

        Assert.Equal(details.BidCount, history.TotalCount);
        Assert.Equal(Enumerable.Range(1, history.TotalCount).Reverse(), history.Items.Select(b => b.Sequence));
        Assert.Equal(history.Items[0].Amount, details.CurrentPrice);
        Assert.Equal(responses.Count(r => r.StatusCode == HttpStatusCode.OK), details.BidderCount);
        Assert.True(history.Items.Zip(history.Items.Skip(1)).All(pair => pair.First.Amount >= pair.Second.Amount));
    }

    [Fact]
    public async Task A_late_bid_extends_the_auction()
    {
        var seller = new Member(factory, "Seller");
        var bidder = new Member(factory, "Sniper");
        var auction = await seller.CreateAuctionAsync(durationMinutes: 5);

        factory.Clock.Advance(TimeSpan.FromMinutes(4.5));
        try
        {
            var result = await bidder.BidOkAsync(auction.Id, 200m);

            Assert.True(result.Extended);
            Assert.True(result.EndsAt > auction.EndsAt);
            Assert.Equal(1, (await seller.GetAuctionAsync(auction.Id)).ExtensionCount);
        }
        finally
        {
            factory.Clock.Reset();
        }
    }

    [Fact]
    public async Task Bidding_activity_shows_up_under_my_bids()
    {
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var bob = new Member(factory, "Bob");
        var auction = await seller.CreateAuctionAsync();
        await alice.BidOkAsync(auction.Id, 300m);
        await bob.BidOkAsync(auction.Id, 400m);

        var aliceActive = await alice.GetAsync<PagedResult<Gavel.Auctions.Application.Me.MyBidDto>>("/api/me/bids?state=Active");
        var bobActive = await bob.GetAsync<PagedResult<Gavel.Auctions.Application.Me.MyBidDto>>("/api/me/bids?state=Active");
        var sellerListings = await seller.GetAsync<PagedResult<AuctionSummaryDto>>("/api/me/auctions");

        Assert.False(Assert.Single(aliceActive.Items, b => b.Auction.Id == auction.Id).IsLeading);
        Assert.True(Assert.Single(bobActive.Items, b => b.Auction.Id == auction.Id).IsLeading);
        Assert.Contains(sellerListings.Items, a => a.Id == auction.Id);
        Assert.DoesNotContain(aliceActive.Items, b => b.Auction.Status != AuctionStatus.Live);
    }
}
