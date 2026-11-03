using Gavel.Notifications.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Notifications.Api.Infrastructure.Persistence.Configurations;

internal sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        builder.ToTable("recipients");
        builder.HasKey(r => r.UserId);
        builder.Property(r => r.Email).HasMaxLength(254);
        builder.Property(r => r.DisplayName).HasMaxLength(60);
    }
}
