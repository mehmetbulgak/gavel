namespace Gavel.Auctions.Domain;

public sealed class Watch
{
    private Watch()
    {
    }

    public Guid AuctionId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Watch Create(Guid auctionId, Guid userId, DateTimeOffset now) => new()
    {
        AuctionId = auctionId,
        UserId = userId,
        CreatedAt = now,
    };
}
