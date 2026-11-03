using System.ComponentModel.DataAnnotations;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class TokenIssuerOptions
{
    public const string SectionName = "Jwt";

    public string? SigningKey { get; set; }

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;

    [Range(1, 365)]
    public int SessionLifetimeDays { get; set; } = 30;
}
