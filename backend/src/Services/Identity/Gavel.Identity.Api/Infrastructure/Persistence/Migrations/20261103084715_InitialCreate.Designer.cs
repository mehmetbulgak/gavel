using System;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gavel.Identity.Api.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20261103084715_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Gavel.Identity.Api.Domain.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTimeOffset>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<Guid?>("ReplacedByTokenId")
                        .HasColumnType("uuid")
                        .HasColumnName("replaced_by_token_id");

                    b.Property<DateTimeOffset?>("RevokedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("revoked_at");

                    b.Property<DateTimeOffset>("SessionStartedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("session_started_at");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("token_hash");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_refresh_tokens");

                    b.HasIndex("TokenHash")
                        .IsUnique()
                        .HasDatabaseName("ix_refresh_tokens_token_hash");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_refresh_tokens_user_id");

                    b.ToTable("refresh_tokens", null, t =>
                        {
                            t.HasCheckConstraint("ck_refresh_tokens_session", "session_started_at <= created_at AND created_at <= expires_at");
                        });
                });

            modelBuilder.Entity("Gavel.Identity.Api.Domain.User", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("display_name");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)")
                        .HasColumnName("email");

                    b.Property<DateTimeOffset?>("LastLoginAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_login_at");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("password_hash");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email");

                    b.ToTable("users", (string)null);
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

            modelBuilder.Entity("Gavel.Identity.Api.Domain.RefreshToken", b =>
                {
                    b.HasOne("Gavel.Identity.Api.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_refresh_tokens_users_user_id");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
