using System.Net;
using System.Net.Http.Json;
using Gavel.TestKit;

namespace Gavel.Gateway.Tests;

public sealed class GatewayTests(GatewayFactory factory) : IClassFixture<GatewayFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/auth/me", "identity")]
    [InlineData("GET", "/api/auctions", "auctions")]
    [InlineData("GET", "/api/auctions/stats", "auctions")]
    [InlineData("GET", "/api/me/bids", "auctions")]
    [InlineData("GET", "/api/notifications", "notifications")]
    [InlineData("POST", "/hubs/auctions/negotiate", "auctions")]
    [InlineData("POST", "/hubs/notifications/negotiate", "notifications")]
    public async Task Requests_are_routed_to_the_owning_service(string method, string path, string service)
    {
        var client = factory.CreateClientFrom($"10.0.0.{Random.Shared.Next(2, 250)}");

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = (await response.Content.ReadFromJsonAsync<EchoResponse>(Ct))!;
        Assert.Equal(service, echo.Service);
        Assert.Equal(path, echo.Path);
    }

    [Theory]
    [InlineData("/.well-known/jwks.json")]
    [InlineData("/health/ready/../../api/auth/me")]
    [InlineData("/internal/anything")]
    public async Task Paths_outside_the_public_api_are_not_proxied(string path)
    {
        var response = await factory.CreateClientFrom("10.1.0.1").GetAsync(path, Ct);

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("identity", (await response.Content.ReadFromJsonAsync<EchoResponse>(Ct))!.Service);
        }
    }

    [Fact]
    public async Task The_service_sees_the_real_client_address()
    {
        var response = await factory.CreateClientFrom("203.0.113.7").GetAsync("/api/auctions", Ct);

        Assert.Equal("203.0.113.7", (await response.Content.ReadFromJsonAsync<EchoResponse>(Ct))!.ForwardedFor);
    }

    [Fact]
    public async Task Sign_in_attempts_are_limited_per_client_without_blocking_others_or_refresh()
    {
        var attacker = factory.CreateClientFrom("198.51.100.10");
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i <= GatewayFactory.SignInLimit; i++)
        {
            statuses.Add((await attacker.PostAsync("/api/auth/login", null, Ct)).StatusCode);
        }

        var otherClient = await factory.CreateClientFrom("198.51.100.11").PostAsync("/api/auth/login", null, Ct);
        var refresh = await attacker.PostAsync("/api/auth/refresh", null, Ct);

        Assert.All(statuses[..^1], s => Assert.Equal(HttpStatusCode.OK, s));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
        Assert.Equal(HttpStatusCode.OK, otherClient.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task Signed_in_users_have_their_own_api_budget_even_behind_one_address()
    {
        var alice = factory.CreateClientFrom("192.0.2.50", TestTokens.Issue(Guid.NewGuid(), "Alice", DateTimeOffset.UtcNow));
        var bob = factory.CreateClientFrom("192.0.2.50", TestTokens.Issue(Guid.NewGuid(), "Bob", DateTimeOffset.UtcNow));

        for (var i = 0; i < GatewayFactory.ApiLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/api/me/bids", Ct)).StatusCode);
        }

        var aliceOver = await alice.GetAsync("/api/me/bids", Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, aliceOver.StatusCode);
        Assert.True(aliceOver.Headers.Contains("Retry-After"));
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/me/bids", Ct)).StatusCode);
    }

    [Fact]
    public async Task Forged_tokens_do_not_buy_a_separate_budget()
    {
        var forged = factory.CreateClientFrom("192.0.2.77", "eyJhbGciOiJub25lIn0.eyJzdWIiOiJ4In0.");

        for (var i = 0; i < GatewayFactory.ApiLimit; i++)
        {
            await forged.GetAsync("/api/auctions", Ct);
        }

        var anonymousFromSameAddress = await factory.CreateClientFrom("192.0.2.77").GetAsync("/api/auctions", Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, anonymousFromSameAddress.StatusCode);
    }

    [Fact]
    public async Task Responses_carry_security_headers_and_hide_the_server()
    {
        var response = await factory.CreateClientFrom("10.2.0.1").GetAsync("/api/auctions", Ct);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.False(response.Headers.Contains("Server"));
    }
}
