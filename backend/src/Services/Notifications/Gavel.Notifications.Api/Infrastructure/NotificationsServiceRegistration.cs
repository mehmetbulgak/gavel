using System.Text.Json.Serialization;
using Gavel.Contracts.Auctions;
using Gavel.Contracts.Identity;
using Gavel.Messaging;
using Gavel.Notifications.Api.Features.Events;
using Gavel.Notifications.Api.Features.Notifications;
using Gavel.Notifications.Api.Infrastructure.Email;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Gavel.Notifications.Api.Realtime;
using Gavel.Persistence;
using Gavel.ServiceDefaults.Security;
using StackExchange.Redis;

namespace Gavel.Notifications.Api.Infrastructure;

internal static class NotificationsServiceRegistration
{
    public const string ServiceName = "notifications";
    public const string DatabaseName = "notificationsdb";
    public const string RedisName = "redis";

    public static WebApplicationBuilder AddNotificationsService(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddGavelDbContext<NotificationsDbContext>(DatabaseName);
        services.AddMessaging(ServiceName)
            .AddConsumer<NotificationsDbContext>()
            .AddHandler<UserRegistered, UserRegisteredHandler>()
            .AddHandler<BidderOutbid, BidderOutbidHandler>()
            .AddHandler<AuctionEnded, AuctionEndedHandler>()
            .AddHandler<AuctionEndingSoon, AuctionEndingSoonHandler>();
        services.AddGavelAuthentication();

        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<EmailComposer>();
        services.AddScoped<EmailDispatchProcessor>();
        services.AddHostedService<EmailDispatchWorker>();

        services.AddScoped<NotificationWriter>();
        services.AddSingleton<NotificationPusher>();
        services.AddScoped<ListNotifications.Handler>();
        services.AddScoped<MarkRead.Handler>();

        var signalR = services
            .AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 1024;
                options.EnableDetailedErrors = false;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        var redis = builder.Configuration.GetConnectionString(RedisName);
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalR.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix = RedisChannel.Literal("gavel.notifications"));
        }

        return builder;
    }
}
