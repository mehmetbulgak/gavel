using Gavel.Notifications.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Notifications.Api.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.EmailStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Title).HasMaxLength(Notification.TitleMaxLength);
        builder.Property(n => n.Body).HasMaxLength(Notification.BodyMaxLength);
        builder.Property(n => n.EmailLastError).HasMaxLength(Notification.ErrorMaxLength);
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
        builder.HasIndex(n => n.UserId).HasFilter("read_at IS NULL").HasDatabaseName("ix_notifications_unread");
        builder.HasIndex(n => new { n.EmailStatus, n.EmailNextAttemptAt }).HasFilter("email_status = 'Pending'");
    }
}
