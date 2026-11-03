namespace Gavel.Notifications.Api.Features.Notifications;

internal sealed record NotificationPage(IReadOnlyList<NotificationDto> Items, int Page, int PageSize, int TotalCount, int UnreadCount);
