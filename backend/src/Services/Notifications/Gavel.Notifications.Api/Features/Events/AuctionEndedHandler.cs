using Gavel.Contracts.Auctions;
using Gavel.Messaging;
using Gavel.Notifications.Api.Domain;

namespace Gavel.Notifications.Api.Features.Events;

internal sealed class AuctionEndedHandler(NotificationWriter notifications) : IIntegrationEventHandler<AuctionEnded>
{
    public Task HandleAsync(AuctionEnded integrationEvent, MessageContext context, CancellationToken ct)
    {
        var title = NotificationTexts.Quote(integrationEvent.AuctionTitle);

        if (integrationEvent is { Outcome: AuctionOutcome.Sold, WinnerId: { } winner, FinalPrice: { } price })
        {
            var finalPrice = NotificationTexts.Money(price);
            notifications.Add(context, integrationEvent.SellerId, NotificationType.AuctionSold, $"{title} sold for {finalPrice}",
                "The auction is over and the winning bidder has been told.", integrationEvent.AuctionId, sendEmail: true);
            notifications.Add(context, winner, NotificationType.AuctionWon, $"You won {title}",
                $"Congratulations! Your winning price is {finalPrice}.", integrationEvent.AuctionId, sendEmail: true);
        }
        else
        {
            var reason = integrationEvent.Outcome == AuctionOutcome.ReserveNotMet
                ? "Bidding did not reach your reserve price."
                : "Nobody placed a bid this time.";
            notifications.Add(context, integrationEvent.SellerId, NotificationType.AuctionUnsold, $"{title} ended without a sale",
                reason, integrationEvent.AuctionId, sendEmail: false);
        }

        foreach (var loser in integrationEvent.LosingBidderIds.Distinct())
        {
            notifications.Add(context, loser, NotificationType.AuctionLost, $"{title} has ended",
                "You didn't win this one. There are plenty more live auctions.", integrationEvent.AuctionId, sendEmail: false);
        }

        return Task.CompletedTask;
    }
}
