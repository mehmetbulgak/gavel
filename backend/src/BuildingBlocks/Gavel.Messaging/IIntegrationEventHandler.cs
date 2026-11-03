using Gavel.Contracts;

namespace Gavel.Messaging;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, MessageContext context, CancellationToken ct);
}
