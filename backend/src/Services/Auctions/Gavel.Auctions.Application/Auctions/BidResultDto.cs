namespace Gavel.Auctions.Application.Auctions;

public sealed record BidResultDto(
    bool IsLeading,
    string Alias,
    decimal YourMaxBid,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    int BidCount,
    DateTimeOffset EndsAt,
    bool Extended);
