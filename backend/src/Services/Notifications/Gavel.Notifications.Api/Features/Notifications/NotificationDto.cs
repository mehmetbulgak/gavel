using Gavel.Notifications.Api.Domain;

namespace Gavel.Notifications.Api.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    Guid? AuctionId,
    DateTimeOffset CreatedAt,
    bool IsRead)
{
    internal static NotificationDto From(Notification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Body,
        notification.AuctionId,
        notification.CreatedAt,
        notification.IsRead);
}
