using Microsoft.EntityFrameworkCore;

namespace Gavel.Messaging.Inbox;

public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}
