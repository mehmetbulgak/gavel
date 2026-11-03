using FluentValidation;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Auctions;

public static class CreateAuction
{
    public sealed record Command(
        string Title,
        string? Description,
        AuctionCategory Category,
        decimal StartingPrice,
        decimal? ReservePrice,
        DateTimeOffset? StartsAt,
        int DurationMinutes);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Title).NotEmpty().MaximumLength(AuctionRules.TitleMaxLength);
            RuleFor(c => c.Description).MaximumLength(AuctionRules.DescriptionMaxLength);
            RuleFor(c => c.Category).IsInEnum();
            RuleFor(c => c.StartingPrice)
                .InclusiveBetween(AuctionRules.MinStartingPrice, AuctionRules.MaxStartingPrice)
                .PrecisionScale(12, 2, ignoreTrailingZeros: true);
            RuleFor(c => c.ReservePrice)
                .GreaterThanOrEqualTo(c => c.StartingPrice)
                .LessThanOrEqualTo(AuctionRules.MaxBidAmount)
                .PrecisionScale(12, 2, ignoreTrailingZeros: true)
                .When(c => c.ReservePrice is not null);
            RuleFor(c => c.DurationMinutes)
                .Must(AuctionRules.DurationsInMinutes.Contains)
                .WithMessage("Choose one of the offered durations.");
        }
    }

    public sealed class Handler(IAuctionsDbContext db, ICurrentUser user, IAuctionLiveUpdates live, TimeProvider clock)
    {
        public async Task<Result<AuctionSummaryDto>> HandleAsync(Command command, CancellationToken ct)
        {
            var sellerId = user.RequiredId;
            var created = await db.InTransactionAsync<Result<Auction>>(async token =>
            {
                await db.LockSellerListingsAsync(sellerId, token);
                var active = await db.Auctions.CountAsync(
                    a => a.SellerId == sellerId && (a.Status == AuctionStatus.Scheduled || a.Status == AuctionStatus.Live),
                    token);
                if (active >= AuctionRules.MaxActiveAuctionsPerSeller)
                {
                    return AuctionErrors.TooManyActiveAuctions;
                }

                var auction = Auction.Create(
                    sellerId,
                    user.DisplayName ?? "Seller",
                    command.Title,
                    command.Description ?? string.Empty,
                    command.Category,
                    command.StartingPrice,
                    command.ReservePrice,
                    command.StartsAt,
                    command.DurationMinutes,
                    clock.GetUtcNow());
                if (auction.IsFailure)
                {
                    return auction.Error!;
                }

                db.Auctions.Add(auction.Value);
                await db.SaveChangesAsync(token);
                return auction.Value;
            }, ct);

            if (created.IsFailure)
            {
                return created.Error!;
            }

            var summary = AuctionSummaryDto.From(created.Value);
            await live.AuctionPublishedAsync(summary, ct);
            return summary;
        }
    }
}
