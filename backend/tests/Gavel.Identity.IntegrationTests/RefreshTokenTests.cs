using System.Net;
using Gavel.Identity.Api.Features.Auth;

namespace Gavel.Identity.IntegrationTests;

[Collection(IdentityCollection.Name)]
public sealed class RefreshTokenTests(IdentityFactory factory) : IDisposable
{
    public void Dispose() => factory.Clock.Reset();

    [Fact]
    public async Task Refresh_rotates_the_token()
    {
        var account = await Account.RegisterAsync(factory);

        var rotated = await Account.RefreshAsync(factory, account.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var next = Account.ReadRefreshCookie(rotated);
        Assert.NotNull(next);
        Assert.NotEqual(account.RefreshToken, next);
    }

    [Fact]
    public async Task Reusing_a_just_rotated_token_fails_without_ending_the_new_session()
    {
        var account = await Account.RegisterAsync(factory);
        var next = Account.ReadRefreshCookie(await Account.RefreshAsync(factory, account.RefreshToken))!;

        var replay = await Account.RefreshAsync(factory, account.RefreshToken);
        var continued = await Account.RefreshAsync(factory, next);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("Auth.RefreshTokenRotated", await replay.ProblemCodeAsync());
        Assert.False(replay.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.OK, continued.StatusCode);
    }

    [Fact]
    public async Task Reusing_an_old_token_after_the_grace_period_revokes_every_session()
    {
        var account = await Account.RegisterAsync(factory);
        var next = Account.ReadRefreshCookie(await Account.RefreshAsync(factory, account.RefreshToken))!;

        factory.Clock.Advance(RefreshSession.ConcurrentRotationGracePeriod + TimeSpan.FromSeconds(1));
        var replay = await Account.RefreshAsync(factory, account.RefreshToken);
        var afterReplay = await Account.RefreshAsync(factory, next);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("Auth.InvalidRefreshToken", await replay.ProblemCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
    }

    [Fact]
    public async Task Parallel_refreshes_with_one_token_never_fork_the_session()
    {
        var account = await Account.RegisterAsync(factory);

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Account.RefreshAsync(factory, account.RefreshToken)));

        var winners = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToList();
        Assert.Single(winners);
        Assert.All(responses.Except(winners), r =>
        {
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
            Assert.False(r.Headers.Contains("Set-Cookie"));
        });
    }

    [Fact]
    public async Task Sessions_end_after_their_absolute_lifetime_even_when_refreshed()
    {
        var account = await Account.RegisterAsync(factory);
        var token = account.RefreshToken;
        var statuses = new List<HttpStatusCode>();

        for (var step = 0; step < 5; step++)
        {
            factory.Clock.Advance(TimeSpan.FromDays(6));
            var response = await Account.RefreshAsync(factory, token);
            statuses.Add(response.StatusCode);
            token = Account.ReadRefreshCookie(response) ?? token;
        }

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Unauthorized],
            statuses);
    }

    [Fact]
    public async Task Unknown_tokens_are_rejected_and_the_cookie_is_cleared()
    {
        var response = await Account.RefreshAsync(factory, "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.InvalidRefreshToken", await response.ProblemCodeAsync());
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith($"{RefreshTokenCookie.Name}=;", StringComparison.Ordinal));
    }
}
