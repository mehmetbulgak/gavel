using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Domain.Tests;

public class ProxyBiddingTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();
    private static readonly Guid Carol = Guid.NewGuid();

    [Fact]
    public void First_bid_opens_at_the_starting_price_whatever_the_maximum()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var placement = new Bidders(auction).Bid(Alice, 500m);

        Assert.Equal(100m, auction.CurrentPrice);
        Assert.Equal(Alice, auction.LeaderId);
        Assert.Equal("Bidder 1", auction.LeaderAlias);
        Assert.Equal(1, auction.BidCount);
        Assert.Equal(110m, auction.MinimumNextBid);
        Assert.True(placement.IsNewParticipant);
        Assert.Null(placement.OutbidUserId);
        Assert.Equal(100m, Assert.Single(placement.NewBids).Amount);
    }

    [Fact]
    public void Bids_below_the_minimum_are_rejected()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);

        Assert.Equal("Bid.TooLow", bidders.Try(Alice, 99.99m).Error?.Code);
        bidders.Bid(Alice, 100m);
        Assert.Equal("Bid.TooLow", bidders.Try(Bob, 109.99m).Error?.Code);
    }

    [Fact]
    public void A_higher_maximum_takes_the_lead_at_one_increment_above_the_previous_maximum()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 300m);

        var placement = bidders.Bid(Bob, 1_000m);

        Assert.Equal(Bob, auction.LeaderId);
        Assert.Equal(310m, auction.CurrentPrice);
        Assert.Equal(Alice, placement.OutbidUserId);
        Assert.Collection(
            placement.NewBids,
            bid => Assert.Equal((Alice, 300m, true), (bid.BidderId, bid.Amount, bid.IsAutomatic)),
            bid => Assert.Equal((Bob, 310m, false), (bid.BidderId, bid.Amount, bid.IsAutomatic)));
        Assert.Equal(3, auction.BidCount);
    }

    [Fact]
    public void The_new_price_never_exceeds_the_new_leaders_maximum()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 300m);

        bidders.Bid(Bob, 305m);

        Assert.Equal(Bob, auction.LeaderId);
        Assert.Equal(305m, auction.CurrentPrice);
    }

    [Fact]
    public void A_lower_maximum_is_outbid_immediately_by_the_leaders_proxy()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 1_000m);

        var placement = bidders.Bid(Bob, 400m);

        Assert.Equal(Alice, auction.LeaderId);
        Assert.Equal(410m, auction.CurrentPrice);
        Assert.Null(placement.OutbidUserId);
        Assert.Collection(
            placement.NewBids,
            bid => Assert.Equal((Bob, 400m, false), (bid.BidderId, bid.Amount, bid.IsAutomatic)),
            bid => Assert.Equal((Alice, 410m, true), (bid.BidderId, bid.Amount, bid.IsAutomatic)));
    }

    [Fact]
    public void Ties_go_to_the_earlier_bidder()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 500m);

        bidders.Bid(Bob, 500m);

        Assert.Equal(Alice, auction.LeaderId);
        Assert.Equal(500m, auction.CurrentPrice);
    }

    [Fact]
    public void The_leader_can_raise_their_maximum_without_moving_the_price()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 200m);
        bidders.Bid(Bob, 150m);

        var raised = bidders.Bid(Alice, 900m);

        Assert.Empty(raised.NewBids);
        Assert.Equal(160m, auction.CurrentPrice);
        Assert.Equal(900m, auction.LeaderMaxBid);
        Assert.Equal(900m, bidders.Participant(Alice).MaxBid);
        Assert.Equal("Bid.MaxNotIncreased", bidders.Try(Alice, 900m).Error?.Code);
    }

    [Fact]
    public void Bidders_keep_their_number_across_bids()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 120m);
        bidders.Bid(Bob, 200m);
        bidders.Bid(Carol, 300m);

        var again = bidders.Bid(Alice, 1_000m);

        Assert.False(again.IsNewParticipant);
        Assert.Equal(1, again.Bidder.Number);
        Assert.Equal(3, auction.ParticipantCount);
        Assert.Equal("Bidder 1", auction.LeaderAlias);
    }

    [Fact]
    public void Sequences_increase_and_prices_never_go_down()
    {
        var auction = AuctionFactory.Live(startingPrice: 50m);
        var bidders = new Bidders(auction);
        var bids = new List<Bid>();
        decimal[] maxima = [60m, 75m, 300m, 120m, 301m, 1_000m, 999m];
        Guid[] who = [Alice, Bob, Carol, Alice, Bob, Alice, Carol];

        for (var i = 0; i < maxima.Length; i++)
        {
            bids.AddRange(bidders.Bid(who[i], maxima[i]).NewBids);
        }

        Assert.Equal(Enumerable.Range(1, bids.Count), bids.Select(b => b.Sequence));
        Assert.True(bids.Zip(bids.Skip(1)).All(pair => pair.Second.Amount >= pair.First.Amount));
        Assert.Equal(Alice, auction.LeaderId);
        Assert.Equal(1_000m, auction.CurrentPrice);
        Assert.True(auction.CurrentPrice <= auction.LeaderMaxBid);
    }

    [Fact]
    public void Sellers_cannot_bid_on_their_own_auction()
    {
        var auction = AuctionFactory.Live();

        Assert.Equal("Auction.SellerCannotBid", new Bidders(auction).Try(AuctionFactory.Seller, 500m).Error?.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(150.555)]
    [InlineData(100_000_001)]
    public void Invalid_amounts_are_rejected(double amount)
    {
        var auction = AuctionFactory.Live();

        Assert.Equal("Bid.InvalidAmount", new Bidders(auction).Try(Alice, (decimal)amount).Error?.Code);
    }

    [Fact]
    public void Ended_auctions_take_no_bids()
    {
        var auction = AuctionFactory.Live(durationMinutes: 5);

        Assert.Equal("Auction.NotOpen", new Bidders(auction).Try(Alice, 500m, AuctionFactory.Now.AddMinutes(5)).Error?.Code);
    }
}
