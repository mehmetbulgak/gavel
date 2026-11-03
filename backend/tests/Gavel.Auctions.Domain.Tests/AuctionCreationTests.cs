using Gavel.Auctions.Domain;

namespace Gavel.Auctions.Domain.Tests;

public class AuctionCreationTests
{
    private static readonly DateTimeOffset Now = AuctionFactory.Now;

    [Fact]
    public void Auction_starting_now_is_live_with_the_starting_price()
    {
        var auction = AuctionFactory.Live(startingPrice: 250m, durationMinutes: 30);

        Assert.Equal(AuctionStatus.Live, auction.Status);
        Assert.Equal(250m, auction.CurrentPrice);
        Assert.Equal(250m, auction.MinimumNextBid);
        Assert.Equal(Now.AddMinutes(30), auction.EndsAt);
        Assert.True(auction.IsOpenForBids(Now));
    }

    [Fact]
    public void Auction_starting_later_is_scheduled_until_started()
    {
        var auction = Create(startsAt: Now.AddHours(2)).Value;

        Assert.Equal(AuctionStatus.Scheduled, auction.Status);
        Assert.False(auction.IsOpenForBids(Now.AddHours(2)));
        Assert.True(auction.Start(Now.AddHours(1)).IsFailure);
        Assert.True(auction.Start(Now.AddHours(2)).IsSuccess);
        Assert.True(auction.IsOpenForBids(Now.AddHours(2)));
    }

    [Theory]
    [InlineData("Shrt", 10, null, 60, "Auction.InvalidTitle")]
    [InlineData("Valid title", 0.5, null, 60, "Auction.InvalidStartingPrice")]
    [InlineData("Valid title", 10.005, null, 60, "Auction.InvalidStartingPrice")]
    [InlineData("Valid title", 10_000_001, null, 60, "Auction.InvalidStartingPrice")]
    [InlineData("Valid title", 100, 99.99, 60, "Auction.InvalidReservePrice")]
    [InlineData("Valid title", 100, 100.001, 60, "Auction.InvalidReservePrice")]
    [InlineData("Valid title", 100, null, 61, "Auction.InvalidDuration")]
    public void Invalid_auctions_are_rejected(string title, double startingPrice, double? reserve, int duration, string code)
    {
        var result = Create(title: title, startingPrice: (decimal)startingPrice, reserve: (decimal?)reserve, duration: duration);

        Assert.Equal(code, result.Error?.Code);
    }

    [Fact]
    public void Auctions_cannot_be_scheduled_more_than_a_week_ahead()
    {
        Assert.Equal("Auction.InvalidStart", Create(startsAt: Now.AddDays(8)).Error?.Code);
    }

    [Fact]
    public void Control_characters_are_rejected_except_line_breaks_in_descriptions()
    {
        Assert.Equal("Auction.InvalidTitle", Create(title: "Line\r\nBreak").Error?.Code);
        Assert.Equal(
            "Auction.InvalidDescription",
            Auction.Create(AuctionFactory.Seller, "Seller", "Valid title", "Bell\u0007", AuctionCategory.Art, 10m, null, null, 60, Now).Error?.Code);
        Assert.True(
            Auction.Create(AuctionFactory.Seller, "Seller", "Valid title", "Line one\nLine two", AuctionCategory.Art, 10m, null, null, 60, Now).IsSuccess);
    }

    [Fact]
    public void Titles_and_descriptions_are_trimmed()
    {
        var auction = Create(title: "   Leica M6   ").Value;

        Assert.Equal("Leica M6", auction.Title);
    }

    private static Gavel.SharedKernel.Result<Auction> Create(
        string title = "Valid title",
        decimal startingPrice = 100m,
        decimal? reserve = null,
        int duration = 60,
        DateTimeOffset? startsAt = null) =>
        Auction.Create(AuctionFactory.Seller, "Seller", title, string.Empty, AuctionCategory.Art, startingPrice, reserve, startsAt, duration, Now);
}
