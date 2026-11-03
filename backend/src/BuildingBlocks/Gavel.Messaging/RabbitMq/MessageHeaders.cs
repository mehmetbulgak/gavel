using System.Text;

namespace Gavel.Messaging.RabbitMq;

internal static class MessageHeaders
{
    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";
    public const string OccurredAt = "occurred-at";
    public const string DeliveryCount = "x-delivery-count";

    public static string? ReadString(IDictionary<string, object?>? headers, string name) =>
        headers is not null && headers.TryGetValue(name, out var value)
            ? value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string text => text,
                _ => null,
            }
            : null;

    public static long ReadLong(IDictionary<string, object?>? headers, string name) =>
        headers is not null && headers.TryGetValue(name, out var value)
            ? value switch
            {
                long number => number,
                int number => number,
                _ => 0,
            }
            : 0;
}
