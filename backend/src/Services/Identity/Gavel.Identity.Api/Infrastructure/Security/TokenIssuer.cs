using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gavel.Identity.Api.Domain;
using Gavel.ServiceDefaults.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class TokenIssuer(
    SigningKeyProvider keys,
    IOptions<TokenIssuerOptions> issuerOptions,
    IOptions<TokenValidationSettings> validation,
    TimeProvider clock) : ITokenIssuer
{
    private const int RefreshTokenBytes = 64;

    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(issuerOptions.Value.RefreshTokenDays);

    public TimeSpan SessionLifetime => TimeSpan.FromDays(issuerOptions.Value.SessionLifetimeDays);

    public AccessToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(issuerOptions.Value.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = validation.Value.Issuer,
            Audience = validation.Value.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = keys.SigningCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(GavelClaims.Subject, user.Id.ToString()),
                new Claim(GavelClaims.Name, user.DisplayName),
                new Claim(GavelClaims.TokenId, Guid.NewGuid().ToString()),
            ]),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }

    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

    public string HashRefreshToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
