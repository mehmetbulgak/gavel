using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Domain.Tests;

internal static class AuctionFactory
{
    public static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
    public static readonly Guid Seller = Guid.NewGuid();

    public static Auction Live(decimal startingPrice = 100m, decimal? reserve = null, int durationMinutes = 60) =>
        Auction.Create(Seller, "Seller", "Vintage camera", "Works perfectly.", AuctionCategory.Electronics, startingPrice, reserve, null, durationMinutes, Now).Value;
}

internal sealed class Bidders(Auction auction)
{
    private readonly Dictionary<Guid, AuctionParticipant> _participants = [];

    public BidPlacement Bid(Guid bidder, decimal max, DateTimeOffset? at = null)
    {
        var result = Try(bidder, max, at);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    public Gavel.SharedKernel.Result<BidPlacement> Try(Guid bidder, decimal max, DateTimeOffset? at = null)
    {
        _participants.TryGetValue(bidder, out var participant);
        var result = auction.PlaceBid(bidder, participant, max, at ?? AuctionFactory.Now.AddMinutes(1));
        if (result.IsSuccess)
        {
            _participants[bidder] = result.Value.Bidder;
        }

        return result;
    }

    public AuctionParticipant Participant(Guid bidder) => _participants[bidder];
}
