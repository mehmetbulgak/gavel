using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Api.Endpoints;

internal sealed record AuctionRulesDto(
    IReadOnlyList<string> Categories,
    IReadOnlyList<int> DurationsInMinutes,
    int TitleMinLength,
    int TitleMaxLength,
    int DescriptionMaxLength,
    decimal MinStartingPrice,
    decimal MaxStartingPrice,
    decimal MaxBidAmount,
    int MaxScheduleAheadMinutes,
    int SnipeProtectionSeconds)
{
    public static readonly AuctionRulesDto Current = new(
        Enum.GetNames<AuctionCategory>(),
        AuctionRules.DurationsInMinutes,
        AuctionRules.TitleMinLength,
        AuctionRules.TitleMaxLength,
        AuctionRules.DescriptionMaxLength,
        AuctionRules.MinStartingPrice,
        AuctionRules.MaxStartingPrice,
        AuctionRules.MaxBidAmount,
        (int)AuctionRules.MaxScheduleAhead.TotalMinutes,
        (int)AuctionRules.SnipeProtectionWindow.TotalSeconds);
}
