using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gavel.Messaging;

public static class MessageSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(object message, Type type) => JsonSerializer.Serialize(message, type, Options);

    public static object Deserialize(ReadOnlySpan<byte> body, Type type) =>
        JsonSerializer.Deserialize(body, type, Options) ?? throw new JsonException("The message body is empty.");
}
