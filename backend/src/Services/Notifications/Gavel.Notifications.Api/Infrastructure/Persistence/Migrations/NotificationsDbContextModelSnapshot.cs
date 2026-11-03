using System;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gavel.Notifications.Api.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(NotificationsDbContext))]
    partial class NotificationsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Gavel.Messaging.Inbox.InboxMessage", b =>
                {
                    b.Property<Guid>("MessageId")
                        .HasColumnType("uuid")
                        .HasColumnName("message_id");

                    b.Property<string>("Consumer")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("consumer");

                    b.Property<DateTimeOffset>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("processed_at");

                    b.HasKey("MessageId", "Consumer")
                        .HasName("pk_inbox_messages");

                    b.HasIndex("ProcessedAt")
                        .HasDatabaseName("ix_inbox_messages_processed_at");

                    b.ToTable("inbox_messages", (string)null);
                });

            modelBuilder.Entity("Gavel.Notifications.Api.Domain.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid?>("AuctionId")
                        .HasColumnType("uuid")
                        .HasColumnName("auction_id");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("body");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<int>("EmailAttempts")
                        .HasColumnType("integer")
                        .HasColumnName("email_attempts");

                    b.Property<string>("EmailLastError")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("email_last_error");

                    b.Property<DateTimeOffset?>("EmailNextAttemptAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("email_next_attempt_at");

                    b.Property<DateTimeOffset?>("EmailSentAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("email_sent_at");

                    b.Property<string>("EmailStatus")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("email_status");

                    b.Property<DateTimeOffset?>("ReadAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("read_at");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("title");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)")
                        .HasColumnName("type");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_notifications");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_notifications_unread")
                        .HasFilter("read_at IS NULL");

                    b.HasIndex("EmailStatus", "EmailNextAttemptAt")
                        .HasDatabaseName("ix_notifications_email_status_email_next_attempt_at")
                        .HasFilter("email_status = 'Pending'");

                    b.HasIndex("UserId", "CreatedAt")
                        .HasDatabaseName("ix_notifications_user_id_created_at");

                    b.ToTable("notifications", (string)null);
                });

            modelBuilder.Entity("Gavel.Notifications.Api.Domain.Recipient", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("character varying(60)")
                        .HasColumnName("display_name");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)")
                        .HasColumnName("email");

                    b.HasKey("UserId")
                        .HasName("pk_recipients");

                    b.ToTable("recipients", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
