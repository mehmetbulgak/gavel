namespace Gavel.Contracts.Auctions;

[IntegrationEvent("auctions.auction-ending-soon.v1")]
public sealed record AuctionEndingSoon(
    Guid AuctionId,
    string AuctionTitle,
    DateTimeOffset EndsAt,
    IReadOnlyList<Guid> RecipientIds) : IIntegrationEvent;
