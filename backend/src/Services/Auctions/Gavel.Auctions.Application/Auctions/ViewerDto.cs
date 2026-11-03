namespace Gavel.Auctions.Application.Auctions;

public sealed record ViewerDto(bool IsSeller, bool IsWatching, bool IsLeading, string? Alias, decimal? MaxBid, bool IsWinner);
