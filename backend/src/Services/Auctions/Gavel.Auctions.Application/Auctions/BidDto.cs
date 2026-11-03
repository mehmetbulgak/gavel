using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Application.Auctions;

public sealed record BidDto(Guid Id, string Alias, decimal Amount, bool IsAutomatic, int Sequence, DateTimeOffset PlacedAt, bool IsMine)
{
    public static BidDto From(Bid bid, Guid? viewerId) =>
        new(bid.Id, bid.BidderAlias, bid.Amount, bid.IsAutomatic, bid.Sequence, bid.PlacedAt, viewerId is not null && bid.BidderId == viewerId);
}
