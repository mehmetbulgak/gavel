using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gavel.Notifications.Api.Realtime;

[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    public const string Path = "/hubs/notifications";
}
