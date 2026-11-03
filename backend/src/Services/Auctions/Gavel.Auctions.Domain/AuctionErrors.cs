using System.Globalization;
using Gavel.SharedKernel;

namespace Gavel.Auctions.Domain;

public static class AuctionErrors
{
    public static readonly Error NotFound = Error.NotFound("Auction.NotFound", "Auction not found.");

    public static readonly Error InvalidTitle = Error.Validation(
        "Auction.InvalidTitle",
        $"Title must be {AuctionRules.TitleMinLength}-{AuctionRules.TitleMaxLength} characters.");

    public static readonly Error InvalidDescription = Error.Validation(
        "Auction.InvalidDescription",
        $"Description can be at most {AuctionRules.DescriptionMaxLength} characters.");

    public static readonly Error InvalidStartingPrice = Error.Validation(
        "Auction.InvalidStartingPrice",
        "Starting price must be between 1 and 10,000,000 with at most two decimals.");

    public static readonly Error InvalidReservePrice = Error.Validation(
        "Auction.InvalidReservePrice",
        "Reserve price must be at least the starting price, at most 100,000,000 and have at most two decimals.");

    public static readonly Error InvalidDuration = Error.Validation("Auction.InvalidDuration", "Choose one of the offered durations.");

    public static readonly Error InvalidStart = Error.Validation(
        "Auction.InvalidStart",
        "An auction can start now or at most 7 days from now.");

    public static readonly Error TooManyActiveAuctions = Error.Conflict(
        "Auction.TooManyActive",
        $"You can have at most {AuctionRules.MaxActiveAuctionsPerSeller} scheduled or live auctions.");

    public static readonly Error NotOpen = Error.Conflict("Auction.NotOpen", "This auction is not taking bids.");

    public static readonly Error SellerCannotBid = Error.Forbidden("Auction.SellerCannotBid", "You cannot bid on your own auction.");

    public static readonly Error InvalidBidAmount = Error.Validation(
        "Bid.InvalidAmount",
        "Bids must be positive, at most 100,000,000 and have at most two decimals.");

    public static readonly Error NotYourAuction = Error.Forbidden("Auction.NotYours", "Only the seller can do this.");

    public static readonly Error CannotCancel = Error.Conflict(
        "Auction.CannotCancel",
        "Only scheduled or live auctions without bids can be cancelled.");

    public static Error BidTooLow(decimal minimum) => Error.Validation(
        "Bid.TooLow",
        string.Create(CultureInfo.InvariantCulture, $"Your maximum bid must be at least {minimum:0.00}."));

    public static Error MaxBidMustIncrease(decimal currentMax) => Error.Validation(
        "Bid.MaxNotIncreased",
        string.Create(CultureInfo.InvariantCulture, $"You are already the highest bidder with a maximum of {currentMax:0.00}. Enter a higher maximum to raise it."));
}
