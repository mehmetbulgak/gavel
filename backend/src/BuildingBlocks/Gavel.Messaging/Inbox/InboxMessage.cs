namespace Gavel.Messaging.Inbox;

public sealed class InboxMessage
{
    public Guid MessageId { get; init; }

    public required string Consumer { get; init; }

    public DateTimeOffset ProcessedAt { get; init; }
}
