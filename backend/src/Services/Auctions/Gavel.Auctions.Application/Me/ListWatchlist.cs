using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Application.Common;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Me;

public static class ListWatchlist
{
    public sealed record Query(int Page = 1, int PageSize = Paging.DefaultPageSize);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Page).ValidPage();
            RuleFor(q => q.PageSize).ValidPageSize();
        }
    }

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user)
    {
        public Task<PagedResult<AuctionSummaryDto>> HandleAsync(Query query, CancellationToken ct)
        {
            var userId = user.RequiredId;
            return db.Watches.AsNoTracking()
                .Where(w => w.UserId == userId)
                .Join(db.Auctions.AsNoTracking(), w => w.AuctionId, a => a.Id, (w, a) => new { w.CreatedAt, Auction = a })
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Auction.Id)
                .Select(x => x.Auction)
                .Select(AuctionSummaryDto.Projection)
                .ToPagedResultAsync(query.Page, query.PageSize, ct);
        }
    }
}
