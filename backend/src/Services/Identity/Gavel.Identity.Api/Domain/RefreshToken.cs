using Gavel.SharedKernel;

namespace Gavel.Identity.Api.Domain;

internal sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset SessionStartedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public uint Version { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, TimeSpan sessionLifetime) =>
        Create(userId, tokenHash, now, sessionStartedAt: now, lifetime, sessionLifetime);

    public RefreshToken Successor(string tokenHash, DateTimeOffset now, TimeSpan lifetime, TimeSpan sessionLifetime) =>
        Create(UserId, tokenHash, now, SessionStartedAt, lifetime, sessionLifetime);

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public bool WasRotatedWithin(TimeSpan window, DateTimeOffset now) =>
        ReplacedByTokenId is not null && RevokedAt is { } revokedAt && now - revokedAt <= window;

    public void Revoke(DateTimeOffset now, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }

    private static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset sessionStartedAt,
        TimeSpan lifetime,
        TimeSpan sessionLifetime)
    {
        var tokenExpiry = now.Add(lifetime);
        var sessionExpiry = sessionStartedAt.Add(sessionLifetime);

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            SessionStartedAt = sessionStartedAt,
            ExpiresAt = tokenExpiry < sessionExpiry ? tokenExpiry : sessionExpiry,
        };
    }
}
