using Gavel.SharedKernel;

namespace Gavel.Auctions.Domain;

public sealed class Bid : Entity
{
    private Bid()
    {
    }

    public Guid AuctionId { get; private set; }

    public Guid BidderId { get; private set; }

    public int BidderNumber { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsAutomatic { get; private set; }

    public int Sequence { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public string BidderAlias => AuctionRules.AliasFor(BidderNumber);

    internal static Bid Record(Guid auctionId, Guid bidderId, int bidderNumber, decimal amount, bool isAutomatic, int sequence, DateTimeOffset now) => new()
    {
        AuctionId = auctionId,
        BidderId = bidderId,
        BidderNumber = bidderNumber,
        Amount = amount,
        IsAutomatic = isAutomatic,
        Sequence = sequence,
        PlacedAt = now,
    };
}
