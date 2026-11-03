namespace Gavel.Messaging;

public sealed class MessageContext(Guid messageId, DateTimeOffset occurredAt)
{
    private readonly List<Func<CancellationToken, Task>> _afterCommit = [];

    public Guid MessageId { get; } = messageId;

    public DateTimeOffset OccurredAt { get; } = occurredAt;

    public void AfterCommit(Func<CancellationToken, Task> action) => _afterCommit.Add(action);

    internal IReadOnlyList<Func<CancellationToken, Task>> AfterCommitActions => _afterCommit;
}
