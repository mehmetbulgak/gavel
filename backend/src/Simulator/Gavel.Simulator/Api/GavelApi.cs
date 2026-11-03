using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Gavel.Simulator.Api;

internal sealed class GavelApi(HttpClient http)
{
    public async Task<AuthResponse?> RegisterAsync(string email, string displayName, string password, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("/api/auth/register", new { email, displayName, password }, ct);
        return response.StatusCode == HttpStatusCode.Conflict ? null : await ReadAsync<AuthResponse>(response, ct);
    }

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("/api/auth/login", new { email, password }, ct);
        return await ReadAsync<AuthResponse>(response, ct);
    }

    public async Task<AuctionPage> LiveAuctionsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<AuctionPage>("/api/auctions?status=Live&sort=EndingSoon&pageSize=60", ct)
        ?? new AuctionPage([], 0);

    public async Task<AuctionPage> MyAuctionsAsync(string token, CancellationToken ct)
    {
        using var request = Authorized(HttpMethod.Get, "/api/me/auctions?pageSize=60", token);
        using var response = await http.SendAsync(request, ct);
        return await ReadAsync<AuctionPage>(response, ct);
    }

    public async Task<AuctionDetails> AuctionAsync(Guid auctionId, string token, CancellationToken ct)
    {
        using var request = Authorized(HttpMethod.Get, $"/api/auctions/{auctionId}", token);
        using var response = await http.SendAsync(request, ct);
        return await ReadAsync<AuctionDetails>(response, ct);
    }

    public async Task CreateAuctionAsync(NewAuction auction, string token, CancellationToken ct)
    {
        using var request = Authorized(HttpMethod.Post, "/api/auctions", token);
        request.Content = JsonContent.Create(auction);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HttpStatusCode> BidAsync(Guid auctionId, decimal maxAmount, string token, CancellationToken ct)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/auctions/{auctionId}/bids", token);
        request.Content = JsonContent.Create(new { maxAmount });
        using var response = await http.SendAsync(request, ct);
        return response.StatusCode;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new InvalidOperationException("The response was empty.");
    }
}
