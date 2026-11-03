using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Features.Auth;

internal static class Logout
{
    public sealed record Command(string? RefreshToken);

    public sealed class Handler(IdentityDbContext db, ITokenIssuer tokens, TimeProvider clock)
    {
        public async Task HandleAsync(Command command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.RefreshToken))
            {
                return;
            }

            var hash = tokens.HashRefreshToken(command.RefreshToken);
            var now = clock.GetUtcNow();

            await db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now), ct);
        }
    }
}
