using System.ComponentModel.DataAnnotations;

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 1025;

    public bool UseStartTls { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@gavel.local";

    [Required]
    public string FromName { get; set; } = "Gavel";

    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = "http://localhost:8080";

    public bool DispatchEnabled { get; set; } = true;

    [Range(1, 3600)]
    public int DispatchIntervalSeconds { get; set; } = 5;

    public bool Enabled => !string.IsNullOrWhiteSpace(Host);
}
