using Gavel.SharedKernel;

namespace Gavel.Auctions.Domain;

public sealed class Auction : Entity
{
    private Auction()
    {
    }

    public Guid SellerId { get; private set; }

    public string SellerName { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public AuctionCategory Category { get; private set; }

    public decimal StartingPrice { get; private set; }

    public decimal? ReservePrice { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public AuctionStatus Status { get; private set; }

    public decimal CurrentPrice { get; private set; }

    public int BidCount { get; private set; }

    public int ParticipantCount { get; private set; }

    public Guid? LeaderId { get; private set; }

    public int? LeaderNumber { get; private set; }

    public decimal? LeaderMaxBid { get; private set; }

    public int ExtensionCount { get; private set; }

    public AuctionOutcome? Outcome { get; private set; }

    public Guid? WinnerId { get; private set; }

    public decimal? FinalPrice { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public DateTimeOffset? EndingSoonNotifiedAt { get; private set; }

    public uint Version { get; private set; }

    public bool HasReserve => ReservePrice is not null;

    public bool ReserveMet => ReservePrice is null || CurrentPrice >= ReservePrice;

    public decimal MinimumNextBid => BidCount == 0 ? StartingPrice : CurrentPrice + BidIncrements.For(CurrentPrice);

    public string? LeaderAlias => LeaderNumber is { } number ? AuctionRules.AliasFor(number) : null;

    public static Result<Auction> Create(
        Guid sellerId,
        string sellerName,
        string title,
        string description,
        AuctionCategory category,
        decimal startingPrice,
        decimal? reservePrice,
        DateTimeOffset? startsAt,
        int durationMinutes,
        DateTimeOffset now)
    {
        title = title.Trim();
        description = description.Trim();

        if (title.Length is < AuctionRules.TitleMinLength or > AuctionRules.TitleMaxLength || title.Any(char.IsControl))
        {
            return AuctionErrors.InvalidTitle;
        }

        if (description.Length > AuctionRules.DescriptionMaxLength || description.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
        {
            return AuctionErrors.InvalidDescription;
        }

        if (!Enum.IsDefined(category))
        {
            return Error.Validation("Auction.InvalidCategory", "Choose a valid category.");
        }

        if (startingPrice is < AuctionRules.MinStartingPrice or > AuctionRules.MaxStartingPrice
            || !AuctionRules.HasValidPrecision(startingPrice))
        {
            return AuctionErrors.InvalidStartingPrice;
        }

        if (reservePrice is { } reserve
            && (reserve < startingPrice || reserve > AuctionRules.MaxBidAmount || !AuctionRules.HasValidPrecision(reserve)))
        {
            return AuctionErrors.InvalidReservePrice;
        }

        if (!AuctionRules.DurationsInMinutes.Contains(durationMinutes))
        {
            return AuctionErrors.InvalidDuration;
        }

        var start = startsAt is { } requested && requested > now ? requested : now;
        if (start - now > AuctionRules.MaxScheduleAhead)
        {
            return AuctionErrors.InvalidStart;
        }

        return new Auction
        {
            SellerId = sellerId,
            SellerName = sellerName,
            Title = title,
            Description = description,
            Category = category,
            StartingPrice = startingPrice,
            ReservePrice = reservePrice,
            StartsAt = start,
            EndsAt = start.AddMinutes(durationMinutes),
            Status = start > now ? AuctionStatus.Scheduled : AuctionStatus.Live,
            CurrentPrice = startingPrice,
            CreatedAt = now,
        };
    }

    public bool IsOpenForBids(DateTimeOffset now) => Status == AuctionStatus.Live && now >= StartsAt && now < EndsAt;

    public Result<BidPlacement> PlaceBid(Guid bidderId, AuctionParticipant? participant, decimal maxAmount, DateTimeOffset now)
    {
        if (!IsOpenForBids(now))
        {
            return AuctionErrors.NotOpen;
        }

        if (bidderId == SellerId)
        {
            return AuctionErrors.SellerCannotBid;
        }

        if (maxAmount <= 0 || maxAmount > AuctionRules.MaxBidAmount || !AuctionRules.HasValidPrecision(maxAmount))
        {
            return AuctionErrors.InvalidBidAmount;
        }

        if (LeaderId == bidderId && participant is not null)
        {
            return RaiseLeaderMaximum(participant, maxAmount, now);
        }

        if (maxAmount < MinimumNextBid)
        {
            return AuctionErrors.BidTooLow(MinimumNextBid);
        }

        var isNew = participant is null;
        var bidder = participant ?? AuctionParticipant.Join(Id, bidderId, ++ParticipantCount, now);
        bidder.RaiseMaxBid(maxAmount);

        var bids = new List<Bid>();
        Guid? outbid = null;

        if (LeaderId is null)
        {
            SetLeader(bidder, maxAmount);
            Record(bids, bidder.BidderId, bidder.Number, ApplyReserve(StartingPrice, maxAmount), isAutomatic: false, now);
        }
        else if (maxAmount > LeaderMaxBid!.Value)
        {
            var (previousId, previousNumber, previousMax) = (LeaderId.Value, LeaderNumber!.Value, LeaderMaxBid.Value);
            if (previousMax > CurrentPrice)
            {
                Record(bids, previousId, previousNumber, previousMax, isAutomatic: true, now);
            }

            SetLeader(bidder, maxAmount);
            var price = Math.Min(maxAmount, previousMax + BidIncrements.For(previousMax));
            Record(bids, bidder.BidderId, bidder.Number, ApplyReserve(price, maxAmount), isAutomatic: false, now);
            outbid = previousId;
        }
        else
        {
            var leaderMax = LeaderMaxBid.Value;
            Record(bids, bidder.BidderId, bidder.Number, maxAmount, isAutomatic: false, now);
            var price = maxAmount == leaderMax ? leaderMax : Math.Min(leaderMax, maxAmount + BidIncrements.For(maxAmount));
            Record(bids, LeaderId.Value, LeaderNumber!.Value, ApplyReserve(price, leaderMax), isAutomatic: true, now);
        }

        return new BidPlacement(bidder, isNew, bids, outbid, ExtendIfSniped(now));
    }

    public Result Start(DateTimeOffset now)
    {
        if (Status != AuctionStatus.Scheduled || now < StartsAt)
        {
            return AuctionErrors.NotOpen;
        }

        Status = AuctionStatus.Live;
        return Result.Success();
    }

    public Result Close(DateTimeOffset now)
    {
        if (Status != AuctionStatus.Live || now < EndsAt)
        {
            return AuctionErrors.NotOpen;
        }

        Status = AuctionStatus.Ended;
        ClosedAt = now;

        if (LeaderId is null)
        {
            Outcome = AuctionOutcome.NoBids;
        }
        else if (!ReserveMet)
        {
            Outcome = AuctionOutcome.ReserveNotMet;
        }
        else
        {
            Outcome = AuctionOutcome.Sold;
            WinnerId = LeaderId;
            FinalPrice = CurrentPrice;
        }

        return Result.Success();
    }

    public Result Cancel(Guid requestedBy, DateTimeOffset now)
    {
        if (requestedBy != SellerId)
        {
            return AuctionErrors.NotYourAuction;
        }

        if (Status is not (AuctionStatus.Scheduled or AuctionStatus.Live) || BidCount > 0)
        {
            return AuctionErrors.CannotCancel;
        }

        Status = AuctionStatus.Cancelled;
        ClosedAt = now;
        return Result.Success();
    }

    public bool TryMarkEndingSoon(DateTimeOffset now)
    {
        if (Status != AuctionStatus.Live || EndingSoonNotifiedAt is not null || EndsAt - now > AuctionRules.EndingSoonWindow)
        {
            return false;
        }

        EndingSoonNotifiedAt = now;
        return true;
    }

    private Result<BidPlacement> RaiseLeaderMaximum(AuctionParticipant leader, decimal maxAmount, DateTimeOffset now)
    {
        if (maxAmount <= LeaderMaxBid)
        {
            return AuctionErrors.MaxBidMustIncrease(LeaderMaxBid!.Value);
        }

        LeaderMaxBid = maxAmount;
        leader.RaiseMaxBid(maxAmount);

        var bids = new List<Bid>();
        var price = ApplyReserve(CurrentPrice, maxAmount);
        if (price != CurrentPrice)
        {
            Record(bids, leader.BidderId, leader.Number, price, isAutomatic: true, now);
        }

        var extended = bids.Count > 0 && ExtendIfSniped(now);
        return new BidPlacement(leader, IsNewParticipant: false, bids, OutbidUserId: null, extended);
    }

    private void SetLeader(AuctionParticipant bidder, decimal maxAmount)
    {
        LeaderId = bidder.BidderId;
        LeaderNumber = bidder.Number;
        LeaderMaxBid = maxAmount;
    }

    private void Record(List<Bid> bids, Guid bidderId, int bidderNumber, decimal amount, bool isAutomatic, DateTimeOffset now)
    {
        BidCount++;
        CurrentPrice = amount;
        bids.Add(Bid.Record(Id, bidderId, bidderNumber, amount, isAutomatic, BidCount, now));
    }

    private decimal ApplyReserve(decimal price, decimal leaderMax) =>
        ReservePrice is { } reserve && leaderMax >= reserve && price < reserve ? reserve : price;

    private bool ExtendIfSniped(DateTimeOffset now)
    {
        if (EndsAt - now >= AuctionRules.SnipeProtectionWindow)
        {
            return false;
        }

        EndsAt = now.Add(AuctionRules.SnipeProtectionWindow);
        ExtensionCount++;
        return true;
    }
}
