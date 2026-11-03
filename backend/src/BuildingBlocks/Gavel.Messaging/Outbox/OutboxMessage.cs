namespace Gavel.Messaging.Outbox;

public sealed class OutboxMessage
{
    public const int MaxErrorLength = 2000;

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Type { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
