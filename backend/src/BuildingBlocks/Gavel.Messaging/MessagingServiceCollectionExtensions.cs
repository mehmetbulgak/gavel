using Gavel.Messaging.Outbox;
using Gavel.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static MessagingBuilder AddMessaging(this IServiceCollection services, string serviceName)
    {
        services.AddOptions<MessagingOptions>()
            .BindConfiguration(MessagingOptions.SectionName)
            .Configure(options => options.ServiceName = serviceName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var registry = new IntegrationEventHandlerRegistry();
        services.AddSingleton(registry);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<OutboxSignal>();
        services.AddSingleton<OutboxSignalInterceptor>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return new MessagingBuilder(services, registry);
    }

    public static DbContextOptionsBuilder UseOutboxSignal(this DbContextOptionsBuilder options, IServiceProvider services) =>
        options.AddInterceptors(services.GetRequiredService<OutboxSignalInterceptor>());
}
