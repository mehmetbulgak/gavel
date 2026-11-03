using Gavel.Notifications.Api.Features.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Gavel.Notifications.Api.Realtime;

internal sealed class NotificationPusher(IHubContext<NotificationHub, INotificationClient> hub)
{
    public Task PushAsync(Guid userId, NotificationDto notification) =>
        hub.Clients.User(userId.ToString()).NotificationReceived(notification);
}
