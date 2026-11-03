using System.Collections.Concurrent;
using System.Reflection;
using Gavel.Contracts;

namespace Gavel.Messaging;

public static class IntegrationEventNames
{
    private static readonly ConcurrentDictionary<Type, string> Names = new();

    public static string Of<TEvent>()
        where TEvent : IIntegrationEvent => Of(typeof(TEvent));

    public static string Of(Type eventType) => Names.GetOrAdd(eventType, static type =>
        type.GetCustomAttribute<IntegrationEventAttribute>()?.Name
        ?? throw new InvalidOperationException($"{type.Name} has no [IntegrationEvent] name."));
}
