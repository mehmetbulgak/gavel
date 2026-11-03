using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class WatchAuction
{
    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user, TimeProvider clock)
    {
        public async Task<Result> WatchAsync(Guid auctionId, CancellationToken ct)
        {
            var userId = user.RequiredId;
            if (!await db.Auctions.AnyAsync(a => a.Id == auctionId, ct))
            {
                return AuctionErrors.NotFound;
            }

            if (!await db.Watches.AnyAsync(w => w.AuctionId == auctionId && w.UserId == userId, ct))
            {
                db.Watches.Add(Watch.Create(auctionId, userId, clock.GetUtcNow()));
                await db.SaveChangesAsync(ct);
            }

            return Result.Success();
        }

        public async Task<Result> UnwatchAsync(Guid auctionId, CancellationToken ct)
        {
            var userId = user.RequiredId;
            await db.Watches.Where(w => w.AuctionId == auctionId && w.UserId == userId).ExecuteDeleteAsync(ct);
            return Result.Success();
        }
    }
}
