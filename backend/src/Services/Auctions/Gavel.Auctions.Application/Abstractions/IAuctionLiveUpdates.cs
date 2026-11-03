using Gavel.Auctions.Application.Auctions;

namespace Gavel.Auctions.Application.Abstractions;

public interface IAuctionLiveUpdates
{
    Task AuctionChangedAsync(AuctionUpdate update, CancellationToken ct);

    Task AuctionPublishedAsync(AuctionSummaryDto auction, CancellationToken ct);
}
