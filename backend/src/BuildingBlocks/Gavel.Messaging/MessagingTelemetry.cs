using System.Diagnostics;

namespace Gavel.Messaging;

public static class MessagingTelemetry
{
    public const string ActivitySourceName = "Gavel.Messaging";

    internal static readonly ActivitySource Source = new(ActivitySourceName);

    internal static ActivityContext ParentFrom(string? traceParent, string? traceState) =>
        ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var context) ? context : default;
}
