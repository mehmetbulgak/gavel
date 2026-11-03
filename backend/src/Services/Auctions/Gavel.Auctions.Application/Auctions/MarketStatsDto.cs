namespace Gavel.Auctions.Application.Auctions;

public sealed record MarketStatsDto(int LiveAuctions, int EndingWithinHour, int BidsLastHour, int SoldLastDay);
