using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Auctions;
using Microsoft.AspNetCore.SignalR;

namespace Gavel.Auctions.Api.Realtime;

internal sealed partial class HubAuctionLiveUpdates(IHubContext<AuctionHub, IAuctionClient> hub, ILogger<HubAuctionLiveUpdates> logger)
    : IAuctionLiveUpdates
{
    public async Task AuctionChangedAsync(AuctionUpdate update, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group(AuctionHub.GroupFor(update.AuctionId)).AuctionChanged(update);
            await hub.Clients.Group(AuctionHub.LobbyGroup).AuctionChanged(update with { NewBids = [] });
        }
        catch (Exception exception)
        {
            LogBroadcastFailed(logger, exception, update.AuctionId);
        }
    }

    public async Task AuctionPublishedAsync(AuctionSummaryDto auction, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group(AuctionHub.LobbyGroup).AuctionPublished(auction);
        }
        catch (Exception exception)
        {
            LogBroadcastFailed(logger, exception, auction.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Broadcasting a live update for auction {AuctionId} failed")]
    private static partial void LogBroadcastFailed(ILogger logger, Exception exception, Guid auctionId);
}
