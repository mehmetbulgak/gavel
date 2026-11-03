using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Auctions.Infrastructure.Persistence.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starting_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reserve_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    bid_count = table.Column<int>(type: "integer", nullable: false),
                    participant_count = table.Column<int>(type: "integer", nullable: false),
                    leader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leader_number = table.Column<int>(type: "integer", nullable: true),
                    leader_max_bid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    extension_count = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ending_soon_notified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.CheckConstraint("ck_auctions_counts", "bid_count >= 0 AND participant_count >= 0 AND extension_count >= 0");
                    table.CheckConstraint("ck_auctions_leader", "leader_max_bid IS NULL OR leader_max_bid >= current_price");
                    table.CheckConstraint("ck_auctions_prices", "starting_price > 0 AND current_price >= starting_price");
                    table.CheckConstraint("ck_auctions_reserve", "reserve_price IS NULL OR reserve_price >= starting_price");
                    table.CheckConstraint("ck_auctions_schedule", "ends_at > starts_at");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    trace_parent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    trace_state = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auction_participants",
                columns: table => new
                {
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    max_bid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_participants", x => new { x.auction_id, x.bidder_id });
                    table.CheckConstraint("ck_auction_participants_number", "number > 0");
                    table.ForeignKey(
                        name: "fk_auction_participants_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_number = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bids", x => x.id);
                    table.CheckConstraint("ck_bids_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "watches",
                columns: table => new
                {
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_watches", x => new { x.auction_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_watches_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auction_participants_auction_id_number",
                table: "auction_participants",
                columns: new[] { "auction_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auction_participants_bidder_id",
                table: "auction_participants",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_created_at",
                table: "auctions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_seller_id_status",
                table: "auctions",
                columns: new[] { "seller_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_status_category",
                table: "auctions",
                columns: new[] { "status", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_status_ends_at",
                table: "auctions",
                columns: new[] { "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_status_starts_at",
                table: "auctions",
                columns: new[] { "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_id_sequence",
                table: "bids",
                columns: new[] { "auction_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bids_bidder_id",
                table: "bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "ix_bids_placed_at",
                table: "bids",
                column: "placed_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at_id",
                table: "outbox_messages",
                columns: new[] { "occurred_at", "id" },
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_watches_user_id",
                table: "watches",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_participants");

            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "watches");

            migrationBuilder.DropTable(
                name: "auctions");
        }
    }
}
