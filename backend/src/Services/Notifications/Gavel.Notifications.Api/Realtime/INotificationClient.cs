using Gavel.Notifications.Api.Features.Notifications;

namespace Gavel.Notifications.Api.Realtime;

public interface INotificationClient
{
    Task NotificationReceived(NotificationDto notification);
}
