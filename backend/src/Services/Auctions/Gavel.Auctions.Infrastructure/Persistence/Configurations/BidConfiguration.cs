using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Auctions.Infrastructure.Persistence.Configurations;

internal sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids", table => table.HasCheckConstraint("ck_bids_amount", "amount > 0"));
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Amount).HasPrecision(12, 2);
        builder.Ignore(b => b.BidderAlias);
        builder.HasIndex(b => new { b.AuctionId, b.Sequence }).IsUnique();
        builder.HasIndex(b => b.BidderId);
        builder.HasIndex(b => b.PlacedAt);
        builder.HasOne<Auction>().WithMany().HasForeignKey(b => b.AuctionId).OnDelete(DeleteBehavior.Cascade);
    }
}
