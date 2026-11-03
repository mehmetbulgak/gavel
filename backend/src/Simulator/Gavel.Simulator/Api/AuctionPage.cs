namespace Gavel.Simulator.Api;

internal sealed record AuctionPage(IReadOnlyList<AuctionSummary> Items, int TotalCount);
