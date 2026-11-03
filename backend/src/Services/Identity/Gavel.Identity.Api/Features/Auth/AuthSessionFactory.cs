using Gavel.Identity.Api.Domain;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;

namespace Gavel.Identity.Api.Features.Auth;

internal sealed class AuthSessionFactory(IdentityDbContext db, ITokenIssuer tokens, TimeProvider clock)
{
    public AuthSession Create(User user, RefreshToken? rotating = null)
    {
        var now = clock.GetUtcNow();
        var rawRefreshToken = tokens.CreateRefreshToken();
        var tokenHash = tokens.HashRefreshToken(rawRefreshToken);
        var refreshToken = rotating is null
            ? RefreshToken.Issue(user.Id, tokenHash, now, tokens.RefreshTokenLifetime, tokens.SessionLifetime)
            : rotating.Successor(tokenHash, now, tokens.RefreshTokenLifetime, tokens.SessionLifetime);

        db.RefreshTokens.Add(refreshToken);
        rotating?.Revoke(now, refreshToken.Id);

        return new AuthSession(tokens.CreateAccessToken(user), rawRefreshToken, refreshToken.ExpiresAt, UserDto.From(user));
    }
}
