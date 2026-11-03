using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Me;

public static class ListMyBids
{
    public sealed record Query(MyBidState State = MyBidState.Active, int Page = 1, int PageSize = Paging.DefaultPageSize);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.State).IsInEnum();
            RuleFor(q => q.Page).ValidPage();
            RuleFor(q => q.PageSize).ValidPageSize();
        }
    }

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user)
    {
        public async Task<PagedResult<MyBidDto>> HandleAsync(Query query, CancellationToken ct)
        {
            var userId = user.RequiredId;
            var rows = db.Participants.AsNoTracking()
                .Where(p => p.BidderId == userId)
                .Join(db.Auctions.AsNoTracking(), p => p.AuctionId, a => a.Id, (p, a) => new { Participant = p, Auction = a });

            rows = query.State switch
            {
                MyBidState.Won => rows.Where(r => r.Auction.WinnerId == userId),
                MyBidState.Lost => rows.Where(r => r.Auction.Status == AuctionStatus.Ended && r.Auction.WinnerId != userId),
                _ => rows.Where(r => r.Auction.Status == AuctionStatus.Live),
            };

            var ordered = query.State == MyBidState.Active
                ? rows.OrderBy(r => r.Auction.EndsAt)
                : rows.OrderByDescending(r => r.Auction.ClosedAt);

            var page = await ordered.ThenBy(r => r.Auction.Id).ToPagedResultAsync(query.Page, query.PageSize, ct);

            return new PagedResult<MyBidDto>(
                page.Items.Select(r => new MyBidDto(
                    AuctionSummaryDto.From(r.Auction),
                    r.Participant.Alias,
                    r.Participant.MaxBid,
                    r.Auction.LeaderId == userId,
                    r.Auction.WinnerId == userId)).ToList(),
                page.Page,
                page.PageSize,
                page.TotalCount);
        }
    }
}
