using Gavel.Messaging;
using Gavel.Notifications.Api.Domain;
using Gavel.Notifications.Api.Features.Notifications;
using Gavel.Notifications.Api.Infrastructure.Email;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Gavel.Notifications.Api.Realtime;
using Microsoft.Extensions.Options;

namespace Gavel.Notifications.Api.Features.Events;

internal sealed class NotificationWriter(
    NotificationsDbContext db,
    NotificationPusher pusher,
    IOptions<EmailOptions> email,
    TimeProvider clock)
{
    public void Add(MessageContext context, Guid userId, NotificationType type, string title, string body, Guid? auctionId, bool sendEmail)
    {
        var notification = Notification.Create(userId, type, title, body, auctionId, sendEmail && email.Value.Enabled, clock.GetUtcNow());
        db.Notifications.Add(notification);
        context.AfterCommit(_ => pusher.PushAsync(userId, NotificationDto.From(notification)));
    }
}
