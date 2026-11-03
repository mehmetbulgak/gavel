using Gavel.Contracts.Auctions;
using Gavel.Messaging;
using Gavel.Notifications.Api.Domain;

namespace Gavel.Notifications.Api.Features.Events;

internal sealed class AuctionEndingSoonHandler(NotificationWriter notifications) : IIntegrationEventHandler<AuctionEndingSoon>
{
    public Task HandleAsync(AuctionEndingSoon integrationEvent, MessageContext context, CancellationToken ct)
    {
        foreach (var recipient in integrationEvent.RecipientIds.Distinct())
        {
            notifications.Add(
                context,
                recipient,
                NotificationType.AuctionEndingSoon,
                $"{NotificationTexts.Quote(integrationEvent.AuctionTitle)} ends soon",
                "Less than five minutes left. A bid in the last two minutes resets the clock to two minutes.",
                integrationEvent.AuctionId,
                sendEmail: false);
        }

        return Task.CompletedTask;
    }
}
