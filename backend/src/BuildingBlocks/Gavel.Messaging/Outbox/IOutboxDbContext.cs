using Microsoft.EntityFrameworkCore;

namespace Gavel.Messaging.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
