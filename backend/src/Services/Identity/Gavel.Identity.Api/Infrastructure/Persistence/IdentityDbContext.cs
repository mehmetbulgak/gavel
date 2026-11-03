using Gavel.Identity.Api.Domain;
using Gavel.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Infrastructure.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options), IOutboxDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.ApplyOutbox();
    }
}
