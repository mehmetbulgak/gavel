using System.Net;
using System.Text.Json;
using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Domain;

namespace Gavel.Auctions.IntegrationTests;

[Collection(AuctionsCollection.Name)]
public sealed class AuctionListingTests(AuctionsFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anyone_can_browse_and_open_a_live_auction()
    {
        var seller = new Member(factory, "Seller Sam");
        var created = await seller.CreateAuctionAsync(title: $"Leica M6 {Guid.NewGuid():N}");

        var anonymous = factory.CreateClient();
        var list = await anonymous.GetAsync($"/api/auctions?search={created.Title[..20]}", Ct);
        var details = await anonymous.GetAsync($"/api/auctions/{created.Id}", Ct);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains((await list.ReadAsync<PagedResult<AuctionSummaryDto>>()).Items, a => a.Id == created.Id);
        var auction = await details.ReadAsync<AuctionDetailsDto>();
        Assert.Equal(AuctionStatus.Live, auction.Status);
        Assert.Equal("Seller Sam", auction.SellerName);
        Assert.Null(auction.Viewer);
    }

    [Fact]
    public async Task The_reserve_price_is_visible_only_to_the_seller()
    {
        var seller = new Member(factory, "Seller");
        var bidder = new Member(factory, "Bidder");
        var auction = await seller.CreateAuctionAsync(startingPrice: 100m, reservePrice: 750m);

        var asSeller = await seller.GetAuctionAsync(auction.Id);
        var asBidder = await bidder.GetAuctionAsync(auction.Id);

        Assert.Equal(750m, asSeller.ReservePrice);
        Assert.True(asSeller.Viewer!.IsSeller);
        Assert.Null(asBidder.ReservePrice);
        Assert.True(asBidder.HasReserve);
        Assert.False(asBidder.ReserveMet);
    }

    [Fact]
    public async Task Scheduled_auctions_are_listed_as_upcoming_until_they_start()
    {
        var seller = new Member(factory, "Seller");
        var auction = await seller.CreateAuctionAsync(startsAt: factory.Clock.GetUtcNow().AddMinutes(30));

        Assert.Equal(AuctionStatus.Scheduled, auction.Status);
        var upcoming = await seller.GetAsync<PagedResult<AuctionSummaryDto>>("/api/auctions?status=Upcoming&pageSize=60");
        Assert.Contains(upcoming.Items, a => a.Id == auction.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await new Member(factory, "Early").BidAsync(auction.Id, 500m)).StatusCode);

        factory.Clock.Advance(TimeSpan.FromMinutes(31));
        try
        {
            await factory.RunLifecycleAsync();
            Assert.Equal(AuctionStatus.Live, (await seller.GetAuctionAsync(auction.Id)).Status);
        }
        finally
        {
            factory.Clock.Reset();
        }
    }

    [Fact]
    public async Task Creating_requires_sign_in_and_valid_input()
    {
        var seller = new Member(factory, "Seller");

        var anonymous = await factory.CreateClient().PostAsync("/api/auctions", null, Ct);
        var badDuration = await seller.PostAsync("/api/auctions", new { title = "Valid title", category = "Art", startingPrice = 10, durationMinutes = 7 });
        var badPrice = await seller.PostAsync("/api/auctions", new { title = "Valid title", category = "Art", startingPrice = 10.001, durationMinutes = 60 });
        var badReserve = await seller.PostAsync("/api/auctions", new { title = "Valid title", category = "Art", startingPrice = 100, reservePrice = 50, durationMinutes = 60 });
        var badCategory = await seller.PostAsync("/api/auctions", new { title = "Valid title", category = "Weapons", startingPrice = 10, durationMinutes = 60 });

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badDuration.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPrice.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badReserve.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
    }

    [Fact]
    public async Task Parallel_listings_cannot_exceed_the_active_auction_limit()
    {
        var seller = new Member(factory, "Busy Seller");
        for (var i = 0; i < AuctionRules.MaxActiveAuctionsPerSeller - 2; i++)
        {
            await seller.CreateAuctionAsync(title: $"Lot number {i}");
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => seller.PostAsync("/api/auctions", new
        {
            title = $"Racing lot {i}",
            category = "Art",
            startingPrice = 10,
            durationMinutes = 60,
        })));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(14, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task The_listing_rules_are_published_for_clients()
    {
        var response = await factory.CreateClient().GetAsync("/api/auctions/rules", Ct);
        using var rules = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = rules.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AuctionRules.DurationsInMinutes, root.GetProperty("durationsInMinutes").EnumerateArray().Select(d => d.GetInt32()));
        Assert.Equal(Enum.GetNames<AuctionCategory>(), root.GetProperty("categories").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(AuctionRules.TitleMaxLength, root.GetProperty("titleMaxLength").GetInt32());
        Assert.Equal(AuctionRules.MaxStartingPrice, root.GetProperty("maxStartingPrice").GetDecimal());
        Assert.Equal((int)AuctionRules.MaxScheduleAhead.TotalMinutes, root.GetProperty("maxScheduleAheadMinutes").GetInt32());
        Assert.Equal((int)AuctionRules.SnipeProtectionWindow.TotalSeconds, root.GetProperty("snipeProtectionSeconds").GetInt32());
    }

    [Theory]
    [InlineData("/api/auctions?pageSize=500")]
    [InlineData("/api/auctions?page=0")]
    [InlineData("/api/auctions?status=Secret")]
    [InlineData("/api/auctions?sort=Random")]
    public async Task Invalid_list_queries_are_rejected(string url)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.CreateClient().GetAsync(url, Ct)).StatusCode);
    }

    [Fact]
    public async Task Overlong_and_hostile_search_terms_are_handled_safely()
    {
        var client = factory.CreateClient();

        var overlong = await client.GetAsync($"/api/auctions?search={new string('a', 101)}", Ct);
        var hostile = await client.GetAsync("/api/auctions?search=%25%27%3B%20DROP%20TABLE%20auctions%3B--", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, overlong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, hostile.StatusCode);
        Assert.Empty((await hostile.ReadAsync<PagedResult<AuctionSummaryDto>>()).Items);
    }

    [Fact]
    public async Task Sellers_can_cancel_only_their_own_auctions_without_bids()
    {
        var seller = new Member(factory, "Seller");
        var stranger = new Member(factory, "Stranger");
        var quiet = await seller.CreateAuctionAsync();
        var busy = await seller.CreateAuctionAsync();
        await stranger.BidOkAsync(busy.Id, 150m);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/auctions/{quiet.Id}/cancel")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await seller.PostAsync($"/api/auctions/{busy.Id}/cancel")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.PostAsync($"/api/auctions/{quiet.Id}/cancel")).StatusCode);
        Assert.Equal(AuctionStatus.Cancelled, (await seller.GetAuctionAsync(quiet.Id)).Status);
    }

    [Fact]
    public async Task Watching_adds_the_auction_to_the_watchlist()
    {
        var seller = new Member(factory, "Seller");
        var watcher = new Member(factory, "Watcher");
        var auction = await seller.CreateAuctionAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await watcher.Client.PutAsync($"/api/auctions/{auction.Id}/watch", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await watcher.Client.PutAsync($"/api/auctions/{auction.Id}/watch", null, Ct)).StatusCode);
        Assert.True((await watcher.GetAuctionAsync(auction.Id)).Viewer!.IsWatching);
        Assert.Single((await watcher.GetAsync<PagedResult<AuctionSummaryDto>>("/api/me/watchlist")).Items, a => a.Id == auction.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await watcher.Client.DeleteAsync($"/api/auctions/{auction.Id}/watch", Ct)).StatusCode);
        Assert.Empty((await watcher.GetAsync<PagedResult<AuctionSummaryDto>>("/api/me/watchlist")).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await watcher.Client.PutAsync($"/api/auctions/{Guid.NewGuid()}/watch", null, Ct)).StatusCode);
    }
}
