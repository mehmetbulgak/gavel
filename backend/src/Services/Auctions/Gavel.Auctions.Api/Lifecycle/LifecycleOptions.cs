using System.ComponentModel.DataAnnotations;

namespace Gavel.Auctions.Api.Lifecycle;

internal sealed class LifecycleOptions
{
    public const string SectionName = "Lifecycle";

    public bool Enabled { get; set; } = true;

    [Range(100, 60_000)]
    public int IntervalMilliseconds { get; set; } = 1000;
}
