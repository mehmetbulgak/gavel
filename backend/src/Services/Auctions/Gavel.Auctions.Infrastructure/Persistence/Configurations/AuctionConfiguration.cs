using Gavel.Auctions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Auctions.Infrastructure.Persistence.Configurations;

internal sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    private const int EnumLength = 20;

    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions", table =>
        {
            table.HasCheckConstraint("ck_auctions_prices", "starting_price > 0 AND current_price >= starting_price");
            table.HasCheckConstraint("ck_auctions_reserve", "reserve_price IS NULL OR reserve_price >= starting_price");
            table.HasCheckConstraint("ck_auctions_leader", "leader_max_bid IS NULL OR leader_max_bid >= current_price");
            table.HasCheckConstraint("ck_auctions_schedule", "ends_at > starts_at");
            table.HasCheckConstraint("ck_auctions_counts", "bid_count >= 0 AND participant_count >= 0 AND extension_count >= 0");
        });

        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Title).HasMaxLength(AuctionRules.TitleMaxLength);
        builder.Property(a => a.Description).HasMaxLength(AuctionRules.DescriptionMaxLength);
        builder.Property(a => a.SellerName).HasMaxLength(60);
        builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(a => a.Outcome).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(a => a.StartingPrice).HasPrecision(12, 2);
        builder.Property(a => a.ReservePrice).HasPrecision(12, 2);
        builder.Property(a => a.CurrentPrice).HasPrecision(12, 2);
        builder.Property(a => a.LeaderMaxBid).HasPrecision(12, 2);
        builder.Property(a => a.FinalPrice).HasPrecision(12, 2);
        builder.Property(a => a.Version).IsRowVersion();

        builder.HasIndex(a => new { a.Status, a.EndsAt });
        builder.HasIndex(a => new { a.Status, a.StartsAt });
        builder.HasIndex(a => new { a.Status, a.Category });
        builder.HasIndex(a => new { a.SellerId, a.Status });
        builder.HasIndex(a => a.CreatedAt);
    }
}
