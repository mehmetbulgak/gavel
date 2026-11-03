using System.Text.Json;

namespace Gavel.Identity.IntegrationTests;

internal static class ProblemReader
{
    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
