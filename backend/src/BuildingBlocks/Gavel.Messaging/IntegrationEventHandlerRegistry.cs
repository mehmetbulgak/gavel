using Gavel.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Messaging;

public sealed class IntegrationEventHandlerRegistry
{
    private readonly Dictionary<string, Registration> _registrations = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> EventNames => _registrations.Keys;

    internal void Add<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var name = IntegrationEventNames.Of<TEvent>();
        if (!_registrations.TryAdd(name, new Registration(typeof(TEvent), InvokeAsync<TEvent, THandler>)))
        {
            throw new InvalidOperationException($"A handler for '{name}' is already registered.");
        }
    }

    internal bool TryGet(string eventName, out Registration registration) =>
        _registrations.TryGetValue(eventName, out registration!);

    private static Task InvokeAsync<TEvent, THandler>(IServiceProvider services, object integrationEvent, MessageContext context, CancellationToken ct)
        where TEvent : IIntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent> =>
        services.GetRequiredService<THandler>().HandleAsync((TEvent)integrationEvent, context, ct);

    internal sealed record Registration(Type EventType, Func<IServiceProvider, object, MessageContext, CancellationToken, Task> InvokeAsync);
}
