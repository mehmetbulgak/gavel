using Gavel.Contracts.Auctions;
using Gavel.Messaging;
using Gavel.Notifications.Api.Domain;

namespace Gavel.Notifications.Api.Features.Events;

internal sealed class BidderOutbidHandler(NotificationWriter notifications) : IIntegrationEventHandler<BidderOutbid>
{
    public Task HandleAsync(BidderOutbid integrationEvent, MessageContext context, CancellationToken ct)
    {
        notifications.Add(
            context,
            integrationEvent.UserId,
            NotificationType.Outbid,
            "You've been outbid",
            $"Someone bid more on {NotificationTexts.Quote(integrationEvent.AuctionTitle)}. The price is now {NotificationTexts.Money(integrationEvent.CurrentPrice)}.",
            integrationEvent.AuctionId,
            sendEmail: true);
        return Task.CompletedTask;
    }
}
