using System.Net;
using System.Net.Http.Json;
using Gavel.Contracts.Identity;
using Gavel.Identity.Api.Features.Auth;
using Gavel.TestKit;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Gavel.Identity.IntegrationTests;

[Collection(IdentityCollection.Name)]
public sealed class RegistrationTests(IdentityFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_returns_an_access_token_and_a_hardened_refresh_cookie()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"New-{Guid.NewGuid():N}@Test.dev", displayName = "Zeynep Kaya", password = Account.Password },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshTokenCookie.Name, StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        Assert.Equal("Zeynep Kaya", auth.User.DisplayName);
        Assert.StartsWith("new-", auth.User.Email, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registering_publishes_a_user_registered_event_through_the_outbox()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);

        var account = await Account.RegisterAsync(factory, displayName: "Event Tester");

        var published = await probe.WaitForAsync<UserRegistered>(e => e.UserId == account.Auth.User.Id);
        Assert.Equal(account.Email, published.Email);
        Assert.Equal("Event Tester", published.DisplayName);
    }

    [Fact]
    public async Task Access_tokens_are_es256_and_carry_no_email()
    {
        var account = await Account.RegisterAsync(factory);

        var token = new JsonWebToken(account.Auth.AccessToken);

        Assert.Equal("ES256", token.Alg);
        Assert.False(string.IsNullOrEmpty(token.Kid));
        Assert.DoesNotContain(token.Claims, c => c.Value.Contains('@', StringComparison.Ordinal));
        Assert.Equal(account.Auth.User.Id.ToString(), token.Subject);
    }

    [Fact]
    public async Task Taken_emails_are_rejected_case_insensitively()
    {
        var account = await Account.RegisterAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email = account.Email.ToUpperInvariant(), displayName = "Copy Cat", password = Account.Password },
            Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Auth.EmailTaken", await response.ProblemCodeAsync());
    }

    [Theory]
    [InlineData("not-an-email", "Valid Name", "Passw0rd!")]
    [InlineData("a@test.dev", "<script>alert(1)</script>", "Passw0rd!")]
    [InlineData("a@test.dev", "X", "Passw0rd!")]
    [InlineData("a@test.dev", "Valid Name", "short1")]
    [InlineData("a@test.dev", "Valid Name", "lettersonly")]
    public async Task Invalid_registrations_are_rejected(string email, string displayName, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new { email, displayName, password }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Null_fields_are_rejected_as_bad_requests()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email = (string?)null, displayName = "Valid Name", password = Account.Password },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
