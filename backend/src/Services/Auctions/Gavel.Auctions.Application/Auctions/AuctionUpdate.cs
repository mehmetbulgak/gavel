using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Application.Auctions;

public sealed record AuctionUpdate(
    Guid AuctionId,
    AuctionStatus Status,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    int BidCount,
    int BidderCount,
    DateTimeOffset EndsAt,
    string? LeaderAlias,
    bool ReserveMet,
    AuctionOutcome? Outcome,
    IReadOnlyList<BidDto> NewBids)
{
    public static AuctionUpdate From(Auction auction, IEnumerable<Bid>? newBids = null) => new(
        auction.Id,
        auction.Status,
        auction.CurrentPrice,
        auction.MinimumNextBid,
        auction.BidCount,
        auction.ParticipantCount,
        auction.EndsAt,
        auction.LeaderAlias,
        auction.ReserveMet,
        auction.Outcome,
        (newBids ?? []).Select(bid => BidDto.From(bid, viewerId: null)).ToList());
}
