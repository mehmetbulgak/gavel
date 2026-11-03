namespace Gavel.Notifications.Api.Domain;

internal sealed class Recipient
{
    private Recipient()
    {
    }

    public Guid UserId { get; private set; }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Recipient Create(Guid userId, string email, string displayName, DateTimeOffset now) => new()
    {
        UserId = userId,
        Email = email,
        DisplayName = displayName,
        CreatedAt = now,
    };
}
