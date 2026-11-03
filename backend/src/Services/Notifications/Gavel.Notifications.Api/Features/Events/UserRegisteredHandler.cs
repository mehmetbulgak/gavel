using Gavel.Contracts.Identity;
using Gavel.Messaging;
using Gavel.Notifications.Api.Domain;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Notifications.Api.Features.Events;

internal sealed class UserRegisteredHandler(NotificationsDbContext db, NotificationWriter notifications, TimeProvider clock)
    : IIntegrationEventHandler<UserRegistered>
{
    public async Task HandleAsync(UserRegistered integrationEvent, MessageContext context, CancellationToken ct)
    {
        if (!await db.Recipients.AnyAsync(r => r.UserId == integrationEvent.UserId, ct))
        {
            db.Recipients.Add(Recipient.Create(integrationEvent.UserId, integrationEvent.Email, integrationEvent.DisplayName, clock.GetUtcNow()));
        }

        notifications.Add(
            context,
            integrationEvent.UserId,
            NotificationType.Welcome,
            $"Welcome to Gavel, {integrationEvent.DisplayName}",
            "Your account is ready. Find something worth bidding on, or list an item of your own.",
            auctionId: null,
            sendEmail: true);
    }
}
