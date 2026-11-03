namespace Gavel.Auctions.Domain;

public sealed record BidPlacement(
    AuctionParticipant Bidder,
    bool IsNewParticipant,
    IReadOnlyList<Bid> NewBids,
    Guid? OutbidUserId,
    bool Extended)
{
    public bool PriceChanged => NewBids.Count > 0;
}
