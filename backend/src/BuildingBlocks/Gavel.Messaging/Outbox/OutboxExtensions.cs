using System.Diagnostics;
using Gavel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Messaging.Outbox;

public static class OutboxExtensions
{
    public static void AddIntegrationEvent<TEvent>(this IOutboxDbContext db, TEvent integrationEvent, DateTimeOffset occurredAt)
        where TEvent : IIntegrationEvent
    {
        var activity = Activity.Current;
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = IntegrationEventNames.Of<TEvent>(),
            Payload = MessageSerializer.Serialize(integrationEvent, typeof(TEvent)),
            OccurredAt = occurredAt,
            TraceParent = activity?.Id,
            TraceState = activity?.TraceStateString,
        });
    }

    public static ModelBuilder ApplyOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Type).HasMaxLength(200);
            builder.Property(m => m.Payload).HasColumnType("jsonb");
            builder.Property(m => m.TraceParent).HasMaxLength(64);
            builder.Property(m => m.TraceState).HasMaxLength(512);
            builder.Property(m => m.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);
            builder.HasIndex(m => new { m.OccurredAt, m.Id }).HasFilter("processed_at IS NULL");
        });

        return modelBuilder;
    }
}
