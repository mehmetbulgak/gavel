using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Common;
using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class ListAuctions
{
    public sealed record Query(
        AuctionListStatus Status = AuctionListStatus.Live,
        AuctionCategory? Category = null,
        string? Search = null,
        AuctionSort Sort = AuctionSort.EndingSoon,
        int Page = 1,
        int PageSize = Paging.DefaultPageSize);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Status).IsInEnum();
            RuleFor(q => q.Category).IsInEnum().When(q => q.Category is not null);
            RuleFor(q => q.Search).MaximumLength(Paging.SearchMaxLength);
            RuleFor(q => q.Sort).IsInEnum();
            RuleFor(q => q.Page).ValidPage();
            RuleFor(q => q.PageSize).ValidPageSize();
        }
    }

    public sealed class Handler(IAuctionsDbContext db)
    {
        public Task<PagedResult<AuctionSummaryDto>> HandleAsync(Query query, CancellationToken ct)
        {
            var status = query.Status switch
            {
                AuctionListStatus.Upcoming => AuctionStatus.Scheduled,
                AuctionListStatus.Ended => AuctionStatus.Ended,
                _ => AuctionStatus.Live,
            };

            var auctions = db.Auctions.AsNoTracking().Where(a => a.Status == status);

            if (query.Category is { } category)
            {
                auctions = auctions.Where(a => a.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLowerInvariant();
                auctions = auctions.Where(a => a.Title.ToLower().Contains(term));
            }

            IOrderedQueryable<Auction> ordered = (query.Sort, query.Status) switch
            {
                (AuctionSort.Newest, _) => auctions.OrderByDescending(a => a.CreatedAt),
                (AuctionSort.PriceLow, _) => auctions.OrderBy(a => a.CurrentPrice).ThenBy(a => a.EndsAt),
                (AuctionSort.PriceHigh, _) => auctions.OrderByDescending(a => a.CurrentPrice).ThenBy(a => a.EndsAt),
                (AuctionSort.MostBids, _) => auctions.OrderByDescending(a => a.BidCount).ThenBy(a => a.EndsAt),
                (_, AuctionListStatus.Upcoming) => auctions.OrderBy(a => a.StartsAt),
                (_, AuctionListStatus.Ended) => auctions.OrderByDescending(a => a.ClosedAt),
                _ => auctions.OrderBy(a => a.EndsAt),
            };

            return ordered
                .ThenBy(a => a.Id)
                .Select(AuctionSummaryDto.Projection)
                .ToPagedResultAsync(query.Page, query.PageSize, ct);
        }
    }
}
