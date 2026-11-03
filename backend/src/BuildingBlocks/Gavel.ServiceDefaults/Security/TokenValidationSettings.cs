using System.ComponentModel.DataAnnotations;

namespace Gavel.ServiceDefaults.Security;

public sealed class TokenValidationSettings
{
    public const string SectionName = "Auth";

    [Required]
    public string Issuer { get; set; } = "gavel-identity";

    [Required]
    public string Audience { get; set; } = "gavel";

    public string? MetadataAddress { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;
}
