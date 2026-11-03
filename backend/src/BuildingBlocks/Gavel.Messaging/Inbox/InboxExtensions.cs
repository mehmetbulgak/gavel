using Microsoft.EntityFrameworkCore;

namespace Gavel.Messaging.Inbox;

public static class InboxExtensions
{
    public static ModelBuilder ApplyInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(m => new { m.MessageId, m.Consumer });
            builder.Property(m => m.Consumer).HasMaxLength(100);
            builder.HasIndex(m => m.ProcessedAt);
        });

        return modelBuilder;
    }
}
