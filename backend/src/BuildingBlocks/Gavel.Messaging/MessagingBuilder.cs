using Gavel.Contracts;
using Gavel.Messaging.Inbox;
using Gavel.Messaging.Outbox;
using Gavel.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Messaging;

public sealed class MessagingBuilder
{
    private readonly IntegrationEventHandlerRegistry _registry;

    internal MessagingBuilder(IServiceCollection services, IntegrationEventHandlerRegistry registry)
    {
        Services = services;
        _registry = registry;
    }

    public IServiceCollection Services { get; }

    public MessagingBuilder AddOutbox<TDbContext>()
        where TDbContext : DbContext, IOutboxDbContext
    {
        Services.AddHostedService<OutboxPublisher<TDbContext>>();
        return this;
    }

    public MessagingBuilder AddConsumer<TDbContext>()
        where TDbContext : DbContext, IInboxDbContext
    {
        Services.AddHostedService<RabbitMqConsumer<TDbContext>>();
        return this;
    }

    public MessagingBuilder AddHandler<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _registry.Add<TEvent, THandler>();
        Services.AddScoped<THandler>();
        return this;
    }
}
