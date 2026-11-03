using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class GetBidHistory
{
    public sealed record Query(int Page = 1, int PageSize = 50);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Page).ValidPage();
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user)
    {
        public async Task<Result<PagedResult<BidDto>>> HandleAsync(Guid auctionId, Query query, CancellationToken ct)
        {
            if (!await db.Auctions.AnyAsync(a => a.Id == auctionId, ct))
            {
                return AuctionErrors.NotFound;
            }

            var viewerId = user.Id;
            var page = await db.Bids.AsNoTracking()
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Sequence)
                .ToPagedResultAsync(query.Page, query.PageSize, ct);

            return new PagedResult<BidDto>(
                page.Items.Select(b => BidDto.From(b, viewerId)).ToList(),
                page.Page,
                page.PageSize,
                page.TotalCount);
        }
    }
}
