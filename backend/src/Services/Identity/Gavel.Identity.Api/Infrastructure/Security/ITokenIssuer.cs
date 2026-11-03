using Gavel.Identity.Api.Domain;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal interface ITokenIssuer
{
    TimeSpan RefreshTokenLifetime { get; }

    TimeSpan SessionLifetime { get; }

    AccessToken CreateAccessToken(User user);

    string CreateRefreshToken();

    string HashRefreshToken(string token);
}
