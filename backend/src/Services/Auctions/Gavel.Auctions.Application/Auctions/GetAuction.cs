using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class GetAuction
{
    public const int RecentBidCount = 20;

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user, TimeProvider clock)
    {
        public async Task<Result<AuctionDetailsDto>> HandleAsync(Guid auctionId, CancellationToken ct)
        {
            var auction = await db.Auctions.AsNoTracking().SingleOrDefaultAsync(a => a.Id == auctionId, ct);
            if (auction is null)
            {
                return AuctionErrors.NotFound;
            }

            var viewerId = user.Id;
            var recentBids = await db.Bids.AsNoTracking()
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Sequence)
                .Take(RecentBidCount)
                .ToListAsync(ct);

            var viewer = viewerId is { } id ? await BuildViewerAsync(auction, id, ct) : null;
            var isSeller = viewer?.IsSeller == true;

            return new AuctionDetailsDto(
                auction.Id,
                auction.Title,
                auction.Description,
                auction.Category,
                auction.Status,
                auction.SellerName,
                auction.StartingPrice,
                auction.CurrentPrice,
                auction.MinimumNextBid,
                BidIncrements.For(auction.CurrentPrice),
                auction.BidCount,
                auction.ParticipantCount,
                auction.StartsAt,
                auction.EndsAt,
                auction.ExtensionCount,
                auction.HasReserve,
                auction.ReserveMet,
                isSeller ? auction.ReservePrice : null,
                auction.LeaderAlias,
                auction.Outcome,
                auction.FinalPrice,
                clock.GetUtcNow(),
                viewer,
                recentBids.Select(b => BidDto.From(b, viewerId)).ToList());
        }

        private async Task<ViewerDto> BuildViewerAsync(Auction auction, Guid viewerId, CancellationToken ct)
        {
            var participant = await db.Participants.AsNoTracking()
                .SingleOrDefaultAsync(p => p.AuctionId == auction.Id && p.BidderId == viewerId, ct);
            var watching = await db.Watches.AnyAsync(w => w.AuctionId == auction.Id && w.UserId == viewerId, ct);

            return new ViewerDto(
                auction.SellerId == viewerId,
                watching,
                auction.LeaderId == viewerId,
                participant?.Alias,
                participant?.MaxBid,
                auction.WinnerId == viewerId);
        }
    }
}
