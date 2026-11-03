using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gavel.Identity.Api.Features.Auth;

namespace Gavel.Identity.IntegrationTests;

internal sealed record Account(HttpClient Client, AuthResponse Auth, string RefreshToken, string Email)
{
    public const string Password = "Passw0rd!";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<Account> RegisterAsync(IdentityFactory factory, string? email = null, string displayName = "Test Bidder")
    {
        email ??= $"user-{Guid.NewGuid():N}@test.dev";
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, displayName, password = Password }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new Account(client, auth, ReadRefreshCookie(response)!, email);
    }

    public static Task<HttpResponseMessage> LoginAsync(IdentityFactory factory, string email, string password) =>
        factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    public static async Task<HttpResponseMessage> RefreshAsync(IdentityFactory factory, string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        return await factory.CreateClient().SendAsync(request, Ct);
    }

    public static string? ReadRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{RefreshTokenCookie.Name}=";
        var value = cookies.FirstOrDefault(c => c.StartsWith(prefix, StringComparison.Ordinal))?.Split(';')[0][prefix.Length..];
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
