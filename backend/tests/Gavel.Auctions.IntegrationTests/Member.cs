using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gavel.Auctions.Application.Auctions;

namespace Gavel.Auctions.IntegrationTests;

internal sealed class Member(AuctionsFactory factory, string name)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; } = name;

    public HttpClient Client => factory.CreateClientFor(Id, Name);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task<AuctionSummaryDto> CreateAuctionAsync(
        string title = "Vintage film camera",
        decimal startingPrice = 100m,
        decimal? reservePrice = null,
        int durationMinutes = 60,
        DateTimeOffset? startsAt = null)
    {
        var response = await PostAsync("/api/auctions", new
        {
            title,
            description = "Fully working, recently serviced.",
            category = "Electronics",
            startingPrice,
            reservePrice,
            startsAt,
            durationMinutes,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuctionSummaryDto>(Json, Ct))!;
    }

    public Task<HttpResponseMessage> BidAsync(Guid auctionId, decimal maxAmount) =>
        PostAsync($"/api/auctions/{auctionId}/bids", new { maxAmount });

    public async Task<BidResultDto> BidOkAsync(Guid auctionId, decimal maxAmount)
    {
        var response = await BidAsync(auctionId, maxAmount);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BidResultDto>(Json, Ct))!;
    }

    public Task<AuctionDetailsDto> GetAuctionAsync(Guid auctionId) => GetAsync<AuctionDetailsDto>($"/api/auctions/{auctionId}");

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) => Client.PostAsJsonAsync(url, body, Json, Ct);
}
