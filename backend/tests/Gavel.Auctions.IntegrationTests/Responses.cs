using System.Net.Http.Json;
using System.Text.Json;

namespace Gavel.Auctions.IntegrationTests;

internal static class Responses
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Member.Json, Ct))!;
}
