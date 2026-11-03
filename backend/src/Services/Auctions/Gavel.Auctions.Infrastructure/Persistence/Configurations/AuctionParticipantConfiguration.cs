using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Auctions.Infrastructure.Persistence.Configurations;

internal sealed class AuctionParticipantConfiguration : IEntityTypeConfiguration<AuctionParticipant>
{
    public void Configure(EntityTypeBuilder<AuctionParticipant> builder)
    {
        builder.ToTable("auction_participants", table => table.HasCheckConstraint("ck_auction_participants_number", "number > 0"));
        builder.HasKey(p => new { p.AuctionId, p.BidderId });
        builder.Property(p => p.MaxBid).HasPrecision(12, 2);
        builder.Ignore(p => p.Alias);
        builder.HasIndex(p => new { p.AuctionId, p.Number }).IsUnique();
        builder.HasIndex(p => p.BidderId);
        builder.HasOne<Auction>().WithMany().HasForeignKey(p => p.AuctionId).OnDelete(DeleteBehavior.Cascade);
    }
}
