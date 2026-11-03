namespace Gavel.Contracts.Auctions;

[IntegrationEvent("auctions.bidder-outbid.v1")]
public sealed record BidderOutbid(Guid AuctionId, string AuctionTitle, Guid UserId, decimal CurrentPrice) : IIntegrationEvent;
