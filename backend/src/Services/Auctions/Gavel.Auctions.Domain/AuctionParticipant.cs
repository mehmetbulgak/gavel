namespace Gavel.Auctions.Domain;

public sealed class AuctionParticipant
{
    private AuctionParticipant()
    {
    }

    public Guid AuctionId { get; private set; }

    public Guid BidderId { get; private set; }

    public int Number { get; private set; }

    public decimal MaxBid { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public string Alias => AuctionRules.AliasFor(Number);

    internal static AuctionParticipant Join(Guid auctionId, Guid bidderId, int number, DateTimeOffset now) => new()
    {
        AuctionId = auctionId,
        BidderId = bidderId,
        Number = number,
        JoinedAt = now,
    };

    internal void RaiseMaxBid(decimal maxBid)
    {
        if (maxBid > MaxBid)
        {
            MaxBid = maxBid;
        }
    }
}
