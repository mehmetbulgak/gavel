using System.Text.Json.Serialization;
using System.Threading.Channels;
using Gavel.Contracts.Auctions;
using Gavel.Contracts.Identity;
using Gavel.Notifications.Api.Domain;
using Gavel.Notifications.Api.Features.Notifications;
using Gavel.TestKit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Notifications.IntegrationTests;

[Collection(NotificationsCollection.Name)]
public sealed class DeliveryTests(NotificationsFactory factory) : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        factory.Email.Fail = false;
        factory.Clock.Reset();
    }

    [Fact]
    public async Task Notifications_are_pushed_live_to_the_signed_in_user()
    {
        var inbox = new Inbox(factory);
        var received = Channel.CreateUnbounded<NotificationDto>();
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/notifications"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(TestTokens.Issue(inbox.UserId, "Live", DateTimeOffset.UtcNow));
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
        connection.On<NotificationDto>("NotificationReceived", n => received.Writer.TryWrite(n));
        await connection.StartAsync(Ct);

        await factory.PublishAsync(new BidderOutbid(Guid.NewGuid(), "Turntable", inbox.UserId, 750m));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var notification = await received.Reader.ReadAsync(timeout.Token);
        Assert.Equal(NotificationType.Outbid, notification.Type);
    }

    [Fact]
    public async Task The_notification_hub_refuses_anonymous_connections()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/notifications"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync(Ct));
    }

    [Fact]
    public async Task Emails_are_sent_with_encoded_content_and_retried_when_smtp_fails()
    {
        var inbox = new Inbox(factory);
        var email = $"{inbox.UserId:N}@test.dev";
        await factory.PublishAsync(new UserRegistered(inbox.UserId, email, "Ece"));
        await factory.PublishAsync(new BidderOutbid(Guid.NewGuid(), "<script>alert(1)</script> lamp", inbox.UserId, 50m));
        await inbox.WaitForAsync(p => p.Items.Count == 2);

        factory.Email.Fail = true;
        await factory.DispatchEmailsAsync();
        Assert.DoesNotContain(factory.Email.Sent, m => m.ToAddress == email);

        factory.Email.Fail = false;
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await factory.DispatchEmailsAsync();

        var sent = factory.Email.Sent.Where(m => m.ToAddress == email).ToList();
        Assert.Equal(2, sent.Count);
        var outbid = Assert.Single(sent, m => m.Subject == "You've been outbid");
        Assert.DoesNotContain("<script>", outbid.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", outbid.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("/auctions/", outbid.TextBody, StringComparison.Ordinal);
    }
}
