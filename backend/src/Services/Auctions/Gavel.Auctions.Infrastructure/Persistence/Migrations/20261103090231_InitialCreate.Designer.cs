using System;
using Gavel.Auctions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gavel.Auctions.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AuctionsDbContext))]
    [Migration("20261103090231_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Gavel.Auctions.Domain.Auction", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("BidCount")
                        .HasColumnType("integer")
                        .HasColumnName("bid_count");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("category");

                    b.Property<DateTimeOffset?>("ClosedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("closed_at");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<decimal>("CurrentPrice")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("current_price");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("description");

                    b.Property<DateTimeOffset?>("EndingSoonNotifiedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("ending_soon_notified_at");

                    b.Property<DateTimeOffset>("EndsAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("ends_at");

                    b.Property<int>("ExtensionCount")
                        .HasColumnType("integer")
                        .HasColumnName("extension_count");

                    b.Property<decimal?>("FinalPrice")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("final_price");

                    b.Property<Guid?>("LeaderId")
                        .HasColumnType("uuid")
                        .HasColumnName("leader_id");

                    b.Property<decimal?>("LeaderMaxBid")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("leader_max_bid");

                    b.Property<int?>("LeaderNumber")
                        .HasColumnType("integer")
                        .HasColumnName("leader_number");

                    b.Property<string>("Outcome")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("outcome");

                    b.Property<int>("ParticipantCount")
                        .HasColumnType("integer")
                        .HasColumnName("participant_count");

                    b.Property<decimal?>("ReservePrice")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("reserve_price");

                    b.Property<Guid>("SellerId")
                        .HasColumnType("uuid")
                        .HasColumnName("seller_id");

                    b.Property<string>("SellerName")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("character varying(60)")
                        .HasColumnName("seller_name");

                    b.Property<decimal>("StartingPrice")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("starting_price");

                    b.Property<DateTimeOffset>("StartsAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("starts_at");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("status");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("title");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.Property<Guid?>("WinnerId")
                        .HasColumnType("uuid")
                        .HasColumnName("winner_id");

                    b.HasKey("Id")
                        .HasName("pk_auctions");

                    b.HasIndex("CreatedAt")
                        .HasDatabaseName("ix_auctions_created_at");

                    b.HasIndex("SellerId", "Status")
                        .HasDatabaseName("ix_auctions_seller_id_status");

                    b.HasIndex("Status", "Category")
                        .HasDatabaseName("ix_auctions_status_category");

                    b.HasIndex("Status", "EndsAt")
                        .HasDatabaseName("ix_auctions_status_ends_at");

                    b.HasIndex("Status", "StartsAt")
                        .HasDatabaseName("ix_auctions_status_starts_at");

                    b.ToTable("auctions", null, t =>
                        {
                            t.HasCheckConstraint("ck_auctions_counts", "bid_count >= 0 AND participant_count >= 0 AND extension_count >= 0");

                            t.HasCheckConstraint("ck_auctions_leader", "leader_max_bid IS NULL OR leader_max_bid >= current_price");

                            t.HasCheckConstraint("ck_auctions_prices", "starting_price > 0 AND current_price >= starting_price");

                            t.HasCheckConstraint("ck_auctions_reserve", "reserve_price IS NULL OR reserve_price >= starting_price");

                            t.HasCheckConstraint("ck_auctions_schedule", "ends_at > starts_at");
                        });
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.AuctionParticipant", b =>
                {
                    b.Property<Guid>("AuctionId")
                        .HasColumnType("uuid")
                        .HasColumnName("auction_id");

                    b.Property<Guid>("BidderId")
                        .HasColumnType("uuid")
                        .HasColumnName("bidder_id");

                    b.Property<DateTimeOffset>("JoinedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("joined_at");

                    b.Property<decimal>("MaxBid")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("max_bid");

                    b.Property<int>("Number")
                        .HasColumnType("integer")
                        .HasColumnName("number");

                    b.HasKey("AuctionId", "BidderId")
                        .HasName("pk_auction_participants");

                    b.HasIndex("BidderId")
                        .HasDatabaseName("ix_auction_participants_bidder_id");

                    b.HasIndex("AuctionId", "Number")
                        .IsUnique()
                        .HasDatabaseName("ix_auction_participants_auction_id_number");

                    b.ToTable("auction_participants", null, t =>
                        {
                            t.HasCheckConstraint("ck_auction_participants_number", "number > 0");
                        });
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.Bid", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<decimal>("Amount")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("amount");

                    b.Property<Guid>("AuctionId")
                        .HasColumnType("uuid")
                        .HasColumnName("auction_id");

                    b.Property<Guid>("BidderId")
                        .HasColumnType("uuid")
                        .HasColumnName("bidder_id");

                    b.Property<int>("BidderNumber")
                        .HasColumnType("integer")
                        .HasColumnName("bidder_number");

                    b.Property<bool>("IsAutomatic")
                        .HasColumnType("boolean")
                        .HasColumnName("is_automatic");

                    b.Property<DateTimeOffset>("PlacedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("placed_at");

                    b.Property<int>("Sequence")
                        .HasColumnType("integer")
                        .HasColumnName("sequence");

                    b.HasKey("Id")
                        .HasName("pk_bids");

                    b.HasIndex("BidderId")
                        .HasDatabaseName("ix_bids_bidder_id");

                    b.HasIndex("PlacedAt")
                        .HasDatabaseName("ix_bids_placed_at");

                    b.HasIndex("AuctionId", "Sequence")
                        .IsUnique()
                        .HasDatabaseName("ix_bids_auction_id_sequence");

                    b.ToTable("bids", null, t =>
                        {
                            t.HasCheckConstraint("ck_bids_amount", "amount > 0");
                        });
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.Watch", b =>
                {
                    b.Property<Guid>("AuctionId")
                        .HasColumnType("uuid")
                        .HasColumnName("auction_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.HasKey("AuctionId", "UserId")
                        .HasName("pk_watches");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_watches_user_id");

                    b.ToTable("watches", (string)null);
                });

            modelBuilder.Entity("Gavel.Messaging.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer")
                        .HasColumnName("attempts");

                    b.Property<string>("LastError")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("last_error");

                    b.Property<DateTimeOffset>("OccurredAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("occurred_at");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("payload");

                    b.Property<DateTimeOffset?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("processed_at");

                    b.Property<string>("TraceParent")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("trace_parent");

                    b.Property<string>("TraceState")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("trace_state");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_outbox_messages");

                    b.HasIndex("OccurredAt", "Id")
                        .HasDatabaseName("ix_outbox_messages_occurred_at_id")
                        .HasFilter("processed_at IS NULL");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.AuctionParticipant", b =>
                {
                    b.HasOne("Gavel.Auctions.Domain.Auction", null)
                        .WithMany()
                        .HasForeignKey("AuctionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_auction_participants_auctions_auction_id");
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.Bid", b =>
                {
                    b.HasOne("Gavel.Auctions.Domain.Auction", null)
                        .WithMany()
                        .HasForeignKey("AuctionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_bids_auctions_auction_id");
                });

            modelBuilder.Entity("Gavel.Auctions.Domain.Watch", b =>
                {
                    b.HasOne("Gavel.Auctions.Domain.Auction", null)
                        .WithMany()
                        .HasForeignKey("AuctionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_watches_auctions_auction_id");
                });
#pragma warning restore 612, 618
        }
    }
}
