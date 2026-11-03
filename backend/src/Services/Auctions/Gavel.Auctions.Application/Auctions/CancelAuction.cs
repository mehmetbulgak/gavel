using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;

namespace Gavel.Auctions.Application.Auctions;

public static class CancelAuction
{
    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user, IAuctionLiveUpdates live, TimeProvider clock)
    {
        public async Task<Result> HandleAsync(Guid auctionId, CancellationToken ct)
        {
            var userId = user.RequiredId;

            var outcome = await db.InTransactionAsync<Result<Auction>>(async token =>
            {
                var auction = await db.FindForUpdateAsync(auctionId, token);
                if (auction is null)
                {
                    return AuctionErrors.NotFound;
                }

                var cancelled = auction.Cancel(userId, clock.GetUtcNow());
                if (cancelled.IsFailure)
                {
                    return cancelled.Error!;
                }

                await db.SaveChangesAsync(token);
                return auction;
            }, ct);

            if (outcome.IsFailure)
            {
                return outcome.Error!;
            }

            await live.AuctionChangedAsync(AuctionUpdate.From(outcome.Value), ct);
            return Result.Success();
        }
    }
}
