namespace Gavel.Contracts.Auctions;

[IntegrationEvent("auctions.auction-ended.v1")]
public sealed record AuctionEnded(
    Guid AuctionId,
    string AuctionTitle,
    Guid SellerId,
    AuctionOutcome Outcome,
    Guid? WinnerId,
    decimal? FinalPrice,
    IReadOnlyList<Guid> LosingBidderIds) : IIntegrationEvent;
