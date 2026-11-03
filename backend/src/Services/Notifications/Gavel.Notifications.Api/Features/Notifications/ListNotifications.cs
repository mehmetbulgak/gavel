using FluentValidation;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Notifications.Api.Features.Notifications;

internal static class ListNotifications
{
    public const int MaxPageSize = 50;

    public sealed record Query(bool UnreadOnly = false, int Page = 1, int PageSize = 20);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Page).InclusiveBetween(1, 1000);
            RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        }
    }

    public sealed class Handler(NotificationsDbContext db, ICurrentUser user)
    {
        public async Task<NotificationPage> HandleAsync(Query query, CancellationToken ct)
        {
            var userId = user.RequiredId;
            var mine = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            var filtered = query.UnreadOnly ? mine.Where(n => n.ReadAt == null) : mine;

            var total = await filtered.CountAsync(ct);
            var unread = await mine.CountAsync(n => n.ReadAt == null, ct);
            var items = await filtered
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return new NotificationPage(items.Select(NotificationDto.From).ToList(), query.Page, query.PageSize, total, unread);
        }
    }
}
