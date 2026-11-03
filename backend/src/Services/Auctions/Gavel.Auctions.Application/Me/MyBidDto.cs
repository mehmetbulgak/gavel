using Gavel.Auctions.Application.Auctions;

namespace Gavel.Auctions.Application.Me;

public sealed record MyBidDto(AuctionSummaryDto Auction, string Alias, decimal MaxBid, bool IsLeading, bool IsWinner);
