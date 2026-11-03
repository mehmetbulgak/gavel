using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Auctions.Infrastructure.Persistence.Configurations;

internal sealed class WatchConfiguration : IEntityTypeConfiguration<Watch>
{
    public void Configure(EntityTypeBuilder<Watch> builder)
    {
        builder.ToTable("watches");
        builder.HasKey(w => new { w.AuctionId, w.UserId });
        builder.HasIndex(w => w.UserId);
        builder.HasOne<Auction>().WithMany().HasForeignKey(w => w.AuctionId).OnDelete(DeleteBehavior.Cascade);
    }
}
