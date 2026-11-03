using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class GetMarketStats
{
    public sealed class Handler(IAuctionsDbContext db, TimeProvider clock)
    {
        public async Task<MarketStatsDto> HandleAsync(CancellationToken ct)
        {
            var now = clock.GetUtcNow();
            var inAnHour = now.AddHours(1);
            var anHourAgo = now.AddHours(-1);
            var aDayAgo = now.AddDays(-1);

            var live = await db.Auctions.CountAsync(a => a.Status == AuctionStatus.Live, ct);
            var endingSoon = await db.Auctions.CountAsync(a => a.Status == AuctionStatus.Live && a.EndsAt <= inAnHour, ct);
            var bids = await db.Bids.CountAsync(b => b.PlacedAt >= anHourAgo && !b.IsAutomatic, ct);
            var sold = await db.Auctions.CountAsync(a => a.Outcome == AuctionOutcome.Sold && a.ClosedAt >= aDayAgo, ct);

            return new MarketStatsDto(live, endingSoon, bids, sold);
        }
    }
}
