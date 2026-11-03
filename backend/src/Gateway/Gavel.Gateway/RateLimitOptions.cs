using System.ComponentModel.DataAnnotations;

namespace Gavel.Gateway;

internal sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    [Range(1, 100_000)]
    public int SignIn { get; set; } = 10;

    [Range(1, 100_000)]
    public int Session { get; set; } = 60;

    [Range(1, 100_000)]
    public int Bids { get; set; } = 60;

    [Range(1, 100_000)]
    public int Listings { get; set; } = 10;

    [Range(1, 100_000)]
    public int Api { get; set; } = 300;

    [Range(1, 100_000)]
    public int Realtime { get; set; } = 600;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
