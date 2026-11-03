using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Features.Auth;

internal static class RefreshSession
{
    public static readonly TimeSpan ConcurrentRotationGracePeriod = TimeSpan.FromSeconds(30);

    public sealed record Command(string? RefreshToken);

    public sealed class Handler(IdentityDbContext db, ITokenIssuer tokens, AuthSessionFactory sessions, TimeProvider clock)
    {
        public async Task<Result<AuthSession>> HandleAsync(Command command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.RefreshToken))
            {
                return AuthErrors.InvalidRefreshToken;
            }

            var hash = tokens.HashRefreshToken(command.RefreshToken);
            var stored = await db.RefreshTokens
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (stored is null)
            {
                return AuthErrors.InvalidRefreshToken;
            }

            var now = clock.GetUtcNow();

            if (stored.IsRevoked)
            {
                if (stored.WasRotatedWithin(ConcurrentRotationGracePeriod, now))
                {
                    return AuthErrors.RefreshTokenAlreadyRotated;
                }

                await RevokeAllSessionsAsync(stored.UserId, now, ct);
                return AuthErrors.InvalidRefreshToken;
            }

            if (!stored.IsActive(now))
            {
                return AuthErrors.InvalidRefreshToken;
            }

            var session = sessions.Create(stored.User, rotating: stored);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return AuthErrors.RefreshTokenAlreadyRotated;
            }

            return session;
        }

        private Task RevokeAllSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken ct) =>
            db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now), ct);
    }
}
