using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Common;

public static class Paging
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;
    public const int MaxPage = 1000;
    public const int SearchMaxLength = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }

    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> rule) => rule.InclusiveBetween(1, MaxPage);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) => rule.InclusiveBetween(1, MaxPageSize);
}
