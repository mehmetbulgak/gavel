using Gavel.Messaging.Inbox;
using Gavel.Notifications.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Notifications.Api.Infrastructure.Persistence;

internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options), IInboxDbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Recipient> Recipients => Set<Recipient>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public async Task<IReadOnlyList<Notification>> LockPendingEmailsAsync(DateTimeOffset now, int limit, CancellationToken ct) =>
        await Notifications.FromSql($"""
            SELECT * FROM notifications
            WHERE email_status = 'Pending' AND email_next_attempt_at <= {now}
            ORDER BY email_next_attempt_at
            LIMIT {limit}
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        modelBuilder.ApplyInbox();
    }
}
