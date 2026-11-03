namespace Gavel.Gateway;

internal sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; }

    public string[] KnownNetworks { get; set; } = [];
}
