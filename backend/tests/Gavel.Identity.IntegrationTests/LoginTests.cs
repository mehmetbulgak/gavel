using System.Net;
using System.Net.Http.Json;
using Gavel.Identity.Api.Features.Auth;

namespace Gavel.Identity.IntegrationTests;

[Collection(IdentityCollection.Name)]
public sealed class LoginTests(IdentityFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_succeeds_with_the_right_password_only()
    {
        var account = await Account.RegisterAsync(factory);

        var success = await Account.LoginAsync(factory, $"  {account.Email.ToUpperInvariant()} ", Account.Password);
        var failure = await Account.LoginAsync(factory, account.Email, "Wrong-passw0rd");
        var unknown = await Account.LoginAsync(factory, "nobody@test.dev", Account.Password);

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.NotNull(Account.ReadRefreshCookie(success));
        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await failure.ProblemCodeAsync(), await unknown.ProblemCodeAsync());
    }

    [Fact]
    public async Task Me_returns_the_signed_in_user_and_requires_a_token()
    {
        var account = await Account.RegisterAsync(factory, displayName: "Me Myself");

        var me = await account.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Ct);
        var anonymous = await factory.CreateClient().GetAsync("/api/auth/me", Ct);

        Assert.Equal("Me Myself", me!.DisplayName);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token_and_clears_the_cookie()
    {
        var account = await Account.RegisterAsync(factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={account.RefreshToken}");

        var logout = await factory.CreateClient().SendAsync(request, Ct);
        var refresh = await Account.RefreshAsync(factory, account.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), c => c.StartsWith($"{RefreshTokenCookie.Name}=;", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}

public sealed class LoginThrottleTests(ThrottledIdentityFactory factory) : IClassFixture<ThrottledIdentityFactory>
{
    [Fact]
    public async Task Failed_sign_ins_pause_only_the_targeted_account()
    {
        var victim = await Account.RegisterAsync(factory);
        var bystander = await Account.RegisterAsync(factory);

        for (var attempt = 1; attempt <= ThrottledIdentityFactory.MaxFailures; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Account.LoginAsync(factory, victim.Email, $"guess-{attempt}x")).StatusCode);
        }

        var blocked = await Account.LoginAsync(factory, victim.Email.ToUpperInvariant(), Account.Password);
        var unaffected = await Account.LoginAsync(factory, bystander.Email, Account.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("Auth.TooManyFailedSignIns", await blocked.ProblemCodeAsync());
        Assert.Equal(HttpStatusCode.OK, unaffected.StatusCode);
    }

    [Fact]
    public async Task Parallel_guesses_cannot_get_past_the_account_limit()
    {
        var victim = await Account.RegisterAsync(factory);

        var responses = await Task.WhenAll(Enumerable.Range(1, 12).Select(i => Account.LoginAsync(factory, victim.Email, $"guess-{i}x")));

        Assert.Equal(ThrottledIdentityFactory.MaxFailures, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Unauthorized), r => Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode));
    }
}
