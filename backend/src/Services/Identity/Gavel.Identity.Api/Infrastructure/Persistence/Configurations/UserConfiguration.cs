using Gavel.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Identity.Api.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength);
        builder.Property(u => u.DisplayName).HasMaxLength(User.DisplayNameMaxLength);
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
