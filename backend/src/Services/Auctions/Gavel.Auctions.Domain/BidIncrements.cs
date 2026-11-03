namespace Gavel.Auctions.Domain;

public static class BidIncrements
{
    public static decimal For(decimal price) => price switch
    {
        < 100m => 5m,
        < 500m => 10m,
        < 1_000m => 25m,
        < 5_000m => 50m,
        < 10_000m => 100m,
        < 50_000m => 250m,
        < 250_000m => 1_000m,
        _ => 5_000m,
    };
}
