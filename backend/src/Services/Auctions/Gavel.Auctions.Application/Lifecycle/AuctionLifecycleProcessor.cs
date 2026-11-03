using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Application.Auctions;
using Gavel.Auctions.Domain;
using Gavel.Contracts.Auctions;
using Gavel.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using ContractOutcome = Gavel.Contracts.Auctions.AuctionOutcome;
using DomainOutcome = Gavel.Auctions.Domain.AuctionOutcome;

namespace Gavel.Auctions.Application.Lifecycle;

public sealed class AuctionLifecycleProcessor(IAuctionsDbContext db, IAuctionLiveUpdates live, TimeProvider clock)
{
    public const int BatchSize = 50;

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var started = await StartDueAsync(ct);
        var closed = await CloseDueAsync(ct);
        var endingSoon = await FlagEndingSoonAsync(ct);
        return started + closed + endingSoon;
    }

    private async Task<int> StartDueAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var started = await db.InTransactionAsync(async token =>
        {
            var due = await db.LockDueToStartAsync(now, BatchSize, token);
            foreach (var auction in due)
            {
                auction.Start(now);
            }

            await db.SaveChangesAsync(token);
            return due;
        }, ct);

        foreach (var auction in started)
        {
            await live.AuctionChangedAsync(AuctionUpdate.From(auction), ct);
            await live.AuctionPublishedAsync(AuctionSummaryDto.From(auction), ct);
        }

        return started.Count;
    }

    private async Task<int> CloseDueAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var closed = await db.InTransactionAsync(async token =>
        {
            var due = await db.LockDueToCloseAsync(now, BatchSize, token);
            var bidders = await ParticipantsOfAsync(due, token);

            foreach (var auction in due)
            {
                auction.Close(now);
                var losers = bidders[auction.Id].Where(id => id != auction.WinnerId).ToList();
                db.AddIntegrationEvent(
                    new AuctionEnded(auction.Id, auction.Title, auction.SellerId, Map(auction.Outcome!.Value), auction.WinnerId, auction.FinalPrice, losers),
                    now);
            }

            await db.SaveChangesAsync(token);
            return due;
        }, ct);

        foreach (var auction in closed)
        {
            await live.AuctionChangedAsync(AuctionUpdate.From(auction), ct);
        }

        return closed.Count;
    }

    private async Task<int> FlagEndingSoonAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return await db.InTransactionAsync(async token =>
        {
            var due = await db.LockEndingSoonAsync(now.Add(AuctionRules.EndingSoonWindow), BatchSize, token);
            var ids = due.Select(a => a.Id).ToList();
            var watchers = await db.Watches
                .Where(w => ids.Contains(w.AuctionId))
                .Select(w => new { w.AuctionId, UserId = w.UserId })
                .ToListAsync(token);
            var bidders = await ParticipantsOfAsync(due, token);

            foreach (var auction in due.Where(a => a.TryMarkEndingSoon(now)))
            {
                var recipients = watchers.Where(w => w.AuctionId == auction.Id).Select(w => w.UserId)
                    .Concat(bidders[auction.Id])
                    .Where(id => id != auction.SellerId)
                    .Distinct()
                    .ToList();

                if (recipients.Count > 0)
                {
                    db.AddIntegrationEvent(new AuctionEndingSoon(auction.Id, auction.Title, auction.EndsAt, recipients), now);
                }
            }

            await db.SaveChangesAsync(token);
            return due.Count;
        }, ct);
    }

    private async Task<ILookup<Guid, Guid>> ParticipantsOfAsync(IReadOnlyList<Auction> auctions, CancellationToken ct)
    {
        var ids = auctions.Select(a => a.Id).ToList();
        var rows = await db.Participants
            .Where(p => ids.Contains(p.AuctionId))
            .Select(p => new { p.AuctionId, p.BidderId })
            .ToListAsync(ct);
        return rows.ToLookup(r => r.AuctionId, r => r.BidderId);
    }

    private static ContractOutcome Map(DomainOutcome outcome) => outcome switch
    {
        DomainOutcome.Sold => ContractOutcome.Sold,
        DomainOutcome.ReserveNotMet => ContractOutcome.ReserveNotMet,
        _ => ContractOutcome.NoBids,
    };
}
