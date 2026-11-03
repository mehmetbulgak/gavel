using Gavel.Notifications.Api.Infrastructure.Persistence;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Notifications.Api.Features.Notifications;

internal static class MarkRead
{
    public static readonly Error NotFound = Error.NotFound("Notification.NotFound", "Notification not found.");

    public sealed class Handler(NotificationsDbContext db, ICurrentUser user, TimeProvider clock)
    {
        public async Task<Result> MarkOneAsync(Guid notificationId, CancellationToken ct)
        {
            var userId = user.RequiredId;
            var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
            if (notification is null)
            {
                return NotFound;
            }

            notification.MarkRead(clock.GetUtcNow());
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }

        public async Task MarkAllAsync(CancellationToken ct)
        {
            var userId = user.RequiredId;
            var now = clock.GetUtcNow();
            await db.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadAt, now), ct);
        }

        public Task<int> UnreadCountAsync(CancellationToken ct)
        {
            var userId = user.RequiredId;
            return db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);
        }
    }
}
