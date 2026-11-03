namespace Gavel.Simulator.Api;

internal sealed record AuctionSummary(
    Guid Id,
    string Title,
    string Category,
    string Status,
    decimal CurrentPrice,
    int BidCount,
    DateTimeOffset EndsAt,
    string SellerName);
