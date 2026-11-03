using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.Contracts.Auctions;
using Gavel.Messaging.Outbox;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class PlaceBid
{
    public sealed record Request(decimal MaxAmount);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(r => r.MaxAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(AuctionRules.MaxBidAmount)
            .PrecisionScale(12, 2, ignoreTrailingZeros: true);
    }

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user, IAuctionLiveUpdates live, TimeProvider clock)
    {
        public async Task<Result<BidResultDto>> HandleAsync(Guid auctionId, Request request, CancellationToken ct)
        {
            var bidderId = user.RequiredId;

            var outcome = await db.InTransactionAsync<Result<(Auction Auction, BidPlacement Placement)>>(async token =>
            {
                var auction = await db.FindForUpdateAsync(auctionId, token);
                if (auction is null)
                {
                    return AuctionErrors.NotFound;
                }

                var participant = await db.Participants.SingleOrDefaultAsync(p => p.AuctionId == auctionId && p.BidderId == bidderId, token);
                var now = clock.GetUtcNow();
                var placement = auction.PlaceBid(bidderId, participant, request.MaxAmount, now);
                if (placement.IsFailure)
                {
                    return placement.Error!;
                }

                if (placement.Value.IsNewParticipant)
                {
                    db.Participants.Add(placement.Value.Bidder);
                }

                db.Bids.AddRange(placement.Value.NewBids);
                if (placement.Value.OutbidUserId is { } outbid)
                {
                    db.AddIntegrationEvent(new BidderOutbid(auction.Id, auction.Title, outbid, auction.CurrentPrice), now);
                }

                await db.SaveChangesAsync(token);
                return (auction, placement.Value);
            }, ct);

            if (outcome.IsFailure)
            {
                return outcome.Error!;
            }

            var (auction, placement) = outcome.Value;
            if (placement.PriceChanged)
            {
                await live.AuctionChangedAsync(AuctionUpdate.From(auction, placement.NewBids), ct);
            }

            return new BidResultDto(
                auction.LeaderId == bidderId,
                placement.Bidder.Alias,
                placement.Bidder.MaxBid,
                auction.CurrentPrice,
                auction.MinimumNextBid,
                auction.BidCount,
                auction.EndsAt,
                placement.Extended);
        }
    }
}
