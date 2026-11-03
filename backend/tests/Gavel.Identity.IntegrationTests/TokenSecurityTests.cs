using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.Identity.IntegrationTests;

[Collection(IdentityCollection.Name)]
public sealed class TokenSecurityTests(IdentityFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Jwks_publishes_only_the_public_key_used_to_sign_tokens()
    {
        var account = await Account.RegisterAsync(factory);

        var json = await factory.CreateClient().GetStringAsync("/.well-known/jwks.json", Ct);
        using var document = JsonDocument.Parse(json);
        var key = document.RootElement.GetProperty("keys").EnumerateArray().Single();

        Assert.False(key.TryGetProperty("d", out _));
        Assert.Equal("EC", key.GetProperty("kty").GetString());
        Assert.Equal(new JsonWebToken(account.Auth.AccessToken).Kid, key.GetProperty("kid").GetString());
    }

    [Fact]
    public async Task Discovery_document_points_at_the_jwks()
    {
        var configuration = await factory.CreateClient().GetFromJsonAsync<JsonObject>("/.well-known/openid-configuration", Ct);

        Assert.Equal("gavel-identity", configuration!["issuer"]!.GetValue<string>());
        Assert.EndsWith("/.well-known/jwks.json", configuration["jwks_uri"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        var account = await Account.RegisterAsync(factory);
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "gavel-identity",
            Audience = "gavel",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity([new Claim("sub", account.Auth.User.Id.ToString())]),
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(otherKey) { KeyId = new JsonWebToken(account.Auth.AccessToken).Kid },
                SecurityAlgorithms.EcdsaSha256),
        });

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(forged));
    }

    [Fact]
    public async Task Unsigned_and_symmetric_tokens_are_rejected()
    {
        var account = await Account.RegisterAsync(factory);
        var payload = new JsonObject
        {
            ["sub"] = account.Auth.User.Id.ToString(),
            ["iss"] = "gavel-identity",
            ["aud"] = "gavel",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
        };
        var unsigned = $"{Encode("""{"alg":"none","typ":"JWT"}""")}.{Encode(payload.ToJsonString())}.";

        var jwks = await factory.CreateClient().GetStringAsync("/.well-known/jwks.json", Ct);
        var hmac = new JsonWebTokenHandler().CreateToken(
            payload.ToJsonString(),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwks)), SecurityAlgorithms.HmacSha256));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(unsigned));
        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(hmac));
    }

    [Fact]
    public async Task Tampered_claims_and_expired_tokens_are_rejected()
    {
        var account = await Account.RegisterAsync(factory);
        var parts = account.Auth.AccessToken.Split('.');
        var payload = JsonNode.Parse(Base64UrlEncoder.Decode(parts[1]))!.AsObject();
        payload["sub"] = Guid.NewGuid().ToString();
        var tampered = $"{parts[0]}.{Encode(payload.ToJsonString())}.{parts[2]}";

        factory.Clock.Advance(TimeSpan.FromMinutes(-16));
        Account expired;
        try
        {
            expired = await Account.RegisterAsync(factory);
        }
        finally
        {
            factory.Clock.Reset();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(tampered));
        Assert.Equal(HttpStatusCode.OK, await GetMeAsync(account.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeAsync(expired.Auth.AccessToken));
    }

    [Fact]
    public async Task Rejections_do_not_explain_why_a_token_failed()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.token");

        var response = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("error_description", response.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_responses_carry_security_headers()
    {
        using var content = new StringContent("{not json", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", content, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Unknown_routes_require_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/not-a-route", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Encode(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));

    private async Task<HttpStatusCode> GetMeAsync(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/auth/me", Ct)).StatusCode;
    }
}
