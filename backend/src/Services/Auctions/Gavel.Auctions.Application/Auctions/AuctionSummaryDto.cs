using System.Linq.Expressions;
using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Application.Auctions;

public sealed record AuctionSummaryDto(
    Guid Id,
    string Title,
    AuctionCategory Category,
    AuctionStatus Status,
    decimal CurrentPrice,
    int BidCount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string SellerName,
    bool HasReserve,
    bool ReserveMet,
    AuctionOutcome? Outcome)
{
    public static readonly Expression<Func<Auction, AuctionSummaryDto>> Projection = a => new AuctionSummaryDto(
        a.Id,
        a.Title,
        a.Category,
        a.Status,
        a.CurrentPrice,
        a.BidCount,
        a.StartsAt,
        a.EndsAt,
        a.SellerName,
        a.ReservePrice != null,
        a.ReservePrice == null || a.CurrentPrice >= a.ReservePrice,
        a.Outcome);

    public static AuctionSummaryDto From(Auction auction) => new(
        auction.Id,
        auction.Title,
        auction.Category,
        auction.Status,
        auction.CurrentPrice,
        auction.BidCount,
        auction.StartsAt,
        auction.EndsAt,
        auction.SellerName,
        auction.HasReserve,
        auction.ReserveMet,
        auction.Outcome);
}
