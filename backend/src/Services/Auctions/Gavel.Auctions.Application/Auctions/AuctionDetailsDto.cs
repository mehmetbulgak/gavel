using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Application.Auctions;

public sealed record AuctionDetailsDto(
    Guid Id,
    string Title,
    string Description,
    AuctionCategory Category,
    AuctionStatus Status,
    string SellerName,
    decimal StartingPrice,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    decimal Increment,
    int BidCount,
    int BidderCount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int ExtensionCount,
    bool HasReserve,
    bool ReserveMet,
    decimal? ReservePrice,
    string? LeaderAlias,
    AuctionOutcome? Outcome,
    decimal? FinalPrice,
    DateTimeOffset ServerTime,
    ViewerDto? Viewer,
    IReadOnlyList<BidDto> RecentBids);
