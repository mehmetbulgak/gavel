using System.Text.Json.Serialization;
using System.Threading.Channels;
using Gavel.Auctions.Application.Auctions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Auctions.IntegrationTests;

[Collection(AuctionsCollection.Name)]
public sealed class RealtimeTests(AuctionsFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Watchers_receive_new_bids_live_without_bidder_identities()
    {
        var seller = new Member(factory, "Seller");
        var alice = new Member(factory, "Alice");
        var auction = await seller.CreateAuctionAsync(startingPrice: 100m);
        var updates = Channel.CreateUnbounded<AuctionUpdate>();
        await using var connection = Connect();
        connection.On<AuctionUpdate>("AuctionChanged", update => updates.Writer.TryWrite(update));
        await connection.StartAsync(Ct);
        await connection.InvokeAsync("SubscribeToAuction", auction.Id, Ct);

        await alice.BidOkAsync(auction.Id, 250m);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var update = await updates.Reader.ReadAsync(timeout.Token);
        Assert.Equal(auction.Id, update.AuctionId);
        Assert.Equal(100m, update.CurrentPrice);
        Assert.Equal("Bidder 1", update.LeaderAlias);
        var bid = Assert.Single(update.NewBids);
        Assert.Equal("Bidder 1", bid.Alias);
        Assert.False(bid.IsMine);
    }

    [Fact]
    public async Task A_connection_cannot_subscribe_to_unlimited_auctions()
    {
        await using var connection = Connect();
        await connection.StartAsync(Ct);

        for (var i = 0; i < 20; i++)
        {
            await connection.InvokeAsync("SubscribeToAuction", Guid.NewGuid(), Ct);
        }

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("SubscribeToAuction", Guid.NewGuid(), Ct));
    }

    private HubConnection Connect() => new HubConnectionBuilder()
        .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/auctions"), options =>
        {
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
        })
        .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .Build();
}
