using System.ComponentModel.DataAnnotations;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class LoginThrottleOptions
{
    public const string SectionName = "LoginThrottling";

    [Range(1, 1000)]
    public int MaxFailuresPerAccount { get; set; } = 10;

    [Range(1, 1440)]
    public int WindowMinutes { get; set; } = 15;
}
