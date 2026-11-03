namespace Gavel.Simulator.Api;

internal sealed record NewAuction(
    string Title,
    string Description,
    string Category,
    decimal StartingPrice,
    decimal? ReservePrice,
    DateTimeOffset? StartsAt,
    int DurationMinutes);
