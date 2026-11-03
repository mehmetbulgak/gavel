using System.ComponentModel.DataAnnotations;

namespace Gavel.Simulator;

internal sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    [Required]
    [Url]
    public string GatewayUrl { get; set; } = "http://gateway";

    [Range(2, 30)]
    public int BotCount { get; set; } = 8;

    [Range(1, 60)]
    public int TargetLiveAuctions { get; set; } = 14;

    [Range(500, 60_000)]
    public int MinBidIntervalMilliseconds { get; set; } = 1000;

    [Range(500, 60_000)]
    public int MaxBidIntervalMilliseconds { get; set; } = 3000;

    public string? BotPassword { get; set; }

    [Required]
    [EmailAddress]
    public string DemoEmail { get; set; } = "demo@gavel.dev";

    [Required]
    public string DemoPassword { get; set; } = "Demo1234!";

    [Required]
    public string DemoDisplayName { get; set; } = "Demo Bidder";
}
