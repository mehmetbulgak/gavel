using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Domain.Tests;

public class ReserveAndClosingTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    [Fact]
    public void A_maximum_above_the_reserve_lifts_the_price_to_the_reserve()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m, reserve: 1_000m);

        new Bidders(auction).Bid(Alice, 1_500m);

        Assert.Equal(1_000m, auction.CurrentPrice);
        Assert.True(auction.ReserveMet);
    }

    [Fact]
    public void A_maximum_below_the_reserve_keeps_the_reserve_unmet()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m, reserve: 1_000m);

        new Bidders(auction).Bid(Alice, 900m);

        Assert.Equal(100m, auction.CurrentPrice);
        Assert.False(auction.ReserveMet);
    }

    [Fact]
    public void Raising_the_leading_maximum_past_the_reserve_meets_it()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m, reserve: 1_000m);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 900m);

        var raised = bidders.Bid(Alice, 1_200m);

        Assert.Equal(1_000m, auction.CurrentPrice);
        Assert.Equal(1_000m, Assert.Single(raised.NewBids).Amount);
        Assert.True(auction.ReserveMet);
    }

    [Fact]
    public void Closing_with_a_leader_sells_at_the_current_price()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m, durationMinutes: 5);
        var bidders = new Bidders(auction);
        bidders.Bid(Alice, 300m);
        bidders.Bid(Bob, 200m);

        Assert.True(auction.Close(auction.EndsAt).IsSuccess);

        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.Equal(AuctionOutcome.Sold, auction.Outcome);
        Assert.Equal(Alice, auction.WinnerId);
        Assert.Equal(210m, auction.FinalPrice);
    }

    [Fact]
    public void Closing_without_bids_or_below_the_reserve_does_not_sell()
    {
        var empty = AuctionFactory.Live(durationMinutes: 5);
        var underReserve = AuctionFactory.Live(startingPrice: 100m, reserve: 500m, durationMinutes: 5);
        new Bidders(underReserve).Bid(Alice, 400m);

        empty.Close(empty.EndsAt);
        underReserve.Close(underReserve.EndsAt);

        Assert.Equal(AuctionOutcome.NoBids, empty.Outcome);
        Assert.Equal(AuctionOutcome.ReserveNotMet, underReserve.Outcome);
        Assert.Null(underReserve.WinnerId);
        Assert.Null(underReserve.FinalPrice);
    }

    [Fact]
    public void Auctions_cannot_close_early_or_twice()
    {
        var auction = AuctionFactory.Live(durationMinutes: 5);

        Assert.True(auction.Close(auction.EndsAt.AddSeconds(-1)).IsFailure);
        Assert.True(auction.Close(auction.EndsAt).IsSuccess);
        Assert.True(auction.Close(auction.EndsAt.AddSeconds(1)).IsFailure);
    }

    [Fact]
    public void Late_bids_extend_the_auction_to_stop_sniping()
    {
        var auction = AuctionFactory.Live(startingPrice: 100m, durationMinutes: 5);
        var bidders = new Bidders(auction);
        var originalEnd = auction.EndsAt;
        var late = originalEnd.AddSeconds(-30);

        var early = bidders.Bid(Alice, 200m, AuctionFactory.Now.AddMinutes(1));
        var sniped = bidders.Bid(Bob, 300m, late);

        Assert.False(early.Extended);
        Assert.True(sniped.Extended);
        Assert.Equal(late.Add(AuctionRules.SnipeProtectionWindow), auction.EndsAt);
        Assert.Equal(1, auction.ExtensionCount);
        Assert.True(auction.IsOpenForBids(originalEnd.AddSeconds(30)));
    }

    [Fact]
    public void Only_the_seller_can_cancel_and_only_before_any_bid()
    {
        var auction = AuctionFactory.Live();
        var withBids = AuctionFactory.Live();
        new Bidders(withBids).Bid(Alice, 500m);

        Assert.Equal("Auction.NotYours", auction.Cancel(Alice, AuctionFactory.Now).Error?.Code);
        Assert.Equal("Auction.CannotCancel", withBids.Cancel(AuctionFactory.Seller, AuctionFactory.Now).Error?.Code);
        Assert.True(auction.Cancel(AuctionFactory.Seller, AuctionFactory.Now).IsSuccess);
        Assert.Equal(AuctionStatus.Cancelled, auction.Status);
        Assert.False(auction.IsOpenForBids(AuctionFactory.Now));
    }

    [Fact]
    public void Ending_soon_is_signalled_once_inside_the_window()
    {
        var auction = AuctionFactory.Live(durationMinutes: 30);

        Assert.False(auction.TryMarkEndingSoon(AuctionFactory.Now));
        Assert.True(auction.TryMarkEndingSoon(auction.EndsAt - AuctionRules.EndingSoonWindow));
        Assert.False(auction.TryMarkEndingSoon(auction.EndsAt.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(99.99, 5)]
    [InlineData(100, 10)]
    [InlineData(999, 25)]
    [InlineData(1_000, 50)]
    [InlineData(9_999, 100)]
    [InlineData(10_000, 250)]
    [InlineData(50_000, 1_000)]
    [InlineData(1_000_000, 5_000)]
    public void Increments_grow_with_the_price(double price, double increment)
    {
        Assert.Equal((decimal)increment, BidIncrements.For((decimal)price));
    }
}
