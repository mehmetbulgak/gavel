namespace Gavel.Auctions.Domain;

public static class AuctionRules
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 2000;
    public const decimal MinStartingPrice = 1m;
    public const decimal MaxStartingPrice = 10_000_000m;
    public const decimal MaxBidAmount = 100_000_000m;
    public const int MaxActiveAuctionsPerSeller = 20;

    public static readonly TimeSpan SnipeProtectionWindow = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan EndingSoonWindow = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxScheduleAhead = TimeSpan.FromDays(7);

    public static readonly IReadOnlyList<int> DurationsInMinutes = [5, 15, 30, 60, 360, 1440, 4320, 10080];

    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, 2) == amount;

    public static string AliasFor(int bidderNumber) => $"Bidder {bidderNumber}";
}
