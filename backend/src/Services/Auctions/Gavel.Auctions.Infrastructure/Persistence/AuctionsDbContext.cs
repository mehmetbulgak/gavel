using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Domain;
using Gavel.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Auctions.Infrastructure.Persistence;

public sealed class AuctionsDbContext(DbContextOptions<AuctionsDbContext> options) : DbContext(options), IAuctionsDbContext
{
    private const int SellerListingsLock = 4_201;

    public DbSet<Auction> Auctions => Set<Auction>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<AuctionParticipant> Participants => Set<AuctionParticipant>();

    public DbSet<Watch> Watches => Set<Watch>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct) =>
        Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            ChangeTracker.Clear();
            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await work(ct);
            await transaction.CommitAsync(ct);
            return result;
        });

    public Task<Auction?> FindForUpdateAsync(Guid auctionId, CancellationToken ct) =>
        Auctions.FromSql($"SELECT *, xmin FROM auctions WHERE id = {auctionId} FOR UPDATE").SingleOrDefaultAsync(ct);

    public Task LockSellerListingsAsync(Guid sellerId, CancellationToken ct) =>
        Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SellerListingsLock}, hashtext({sellerId.ToString()}))", ct);

    public async Task<IReadOnlyList<Auction>> LockDueToStartAsync(DateTimeOffset now, int limit, CancellationToken ct) =>
        await Auctions.FromSql($"""
            SELECT *, xmin FROM auctions
            WHERE status = 'Scheduled' AND starts_at <= {now}
            ORDER BY starts_at
            LIMIT {limit}
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);

    public async Task<IReadOnlyList<Auction>> LockDueToCloseAsync(DateTimeOffset now, int limit, CancellationToken ct) =>
        await Auctions.FromSql($"""
            SELECT *, xmin FROM auctions
            WHERE status = 'Live' AND ends_at <= {now}
            ORDER BY ends_at
            LIMIT {limit}
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);

    public async Task<IReadOnlyList<Auction>> LockEndingSoonAsync(DateTimeOffset threshold, int limit, CancellationToken ct) =>
        await Auctions.FromSql($"""
            SELECT *, xmin FROM auctions
            WHERE status = 'Live' AND ending_soon_notified_at IS NULL AND ends_at <= {threshold}
            ORDER BY ends_at
            LIMIT {limit}
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuctionsDbContext).Assembly);
        modelBuilder.ApplyOutbox();
    }
}
