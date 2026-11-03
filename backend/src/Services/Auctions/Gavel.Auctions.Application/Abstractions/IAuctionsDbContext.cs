using Gavel.Auctions.Domain;
using Gavel.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Application.Abstractions;

public interface IAuctionsDbContext : IOutboxDbContext
{
    DbSet<Auction> Auctions { get; }

    DbSet<Bid> Bids { get; }

    DbSet<AuctionParticipant> Participants { get; }

    DbSet<Watch> Watches { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct);

    Task<Auction?> FindForUpdateAsync(Guid auctionId, CancellationToken ct);

    Task LockSellerListingsAsync(Guid sellerId, CancellationToken ct);

    Task<IReadOnlyList<Auction>> LockDueToStartAsync(DateTimeOffset now, int limit, CancellationToken ct);

    Task<IReadOnlyList<Auction>> LockDueToCloseAsync(DateTimeOffset now, int limit, CancellationToken ct);

    Task<IReadOnlyList<Auction>> LockEndingSoonAsync(DateTimeOffset threshold, int limit, CancellationToken ct);
}
