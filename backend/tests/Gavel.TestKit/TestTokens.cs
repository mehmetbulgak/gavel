using System.Security.Claims;
using System.Security.Cryptography;
using Gavel.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.TestKit;

public static class TestTokens
{
    public const string Issuer = "gavel-identity";
    public const string Audience = "gavel";

    private static readonly ECDsaSecurityKey Key = new(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test-key" };
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public static string Issue(Guid userId, string displayName, DateTimeOffset now, TimeSpan? lifetime = null) =>
        Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime ?? TimeSpan.FromMinutes(15)).UtcDateTime,
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.EcdsaSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(GavelClaims.Subject, userId.ToString()),
                new Claim(GavelClaims.Name, displayName),
                new Claim(GavelClaims.TokenId, Guid.NewGuid().ToString()),
            ]),
        });

    public static IServiceCollection AcceptTestTokens(this IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.IssuerSigningKey = Key);
}
