using Gavel.Auctions.Application.Auctions;

namespace Gavel.Auctions.Api.Realtime;

public interface IAuctionClient
{
    Task AuctionChanged(AuctionUpdate update);

    Task AuctionPublished(AuctionSummaryDto auction);
}
