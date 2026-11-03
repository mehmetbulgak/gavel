namespace Gavel.Simulator.Api;

internal sealed record AuctionViewer(bool IsSeller, bool IsLeading, decimal? MaxBid);
