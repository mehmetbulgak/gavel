namespace Gavel.Simulator.Api;

internal sealed record AuctionDetails(Guid Id, string Status, decimal CurrentPrice, decimal MinimumNextBid, DateTimeOffset EndsAt, AuctionViewer? Viewer);
