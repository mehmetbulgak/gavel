using System.Collections.Concurrent;
using Gavel.Contracts;
using Gavel.Notifications.Api.Infrastructure.Email;
using Gavel.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Notifications.IntegrationTests;

public sealed class NotificationsFactory : ServiceFactory<Program>, IAsyncLifetime
{
    internal FakeEmailSender Email { get; } = new();

    protected override string DatabaseConnectionName => "notificationsdb";

    protected override string ServiceName => "notifications";

    public async ValueTask InitializeAsync() => await StartAsync();

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    public Task<Guid> PublishAsync<TEvent>(TEvent integrationEvent, Guid? messageId = null)
        where TEvent : IIntegrationEvent =>
        TestPublisher.PublishWhenRoutableAsync(RabbitMqConnectionString, Exchange, integrationEvent, messageId);

    public async Task<int> DispatchEmailsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<EmailDispatchProcessor>().RunOnceAsync(TestContext.Current.CancellationToken);
    }

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton<IEmailSender>(Email);
}

internal sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public bool Fail { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (Fail)
        {
            throw new InvalidOperationException("SMTP is down.");
        }

        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class NotificationsCollection : ICollectionFixture<NotificationsFactory>
{
    public const string Name = "notifications";
}
