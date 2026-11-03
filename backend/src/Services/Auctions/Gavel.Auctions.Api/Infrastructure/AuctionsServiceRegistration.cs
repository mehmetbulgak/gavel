using System.Text.Json.Serialization;
using Gavel.Auctions.Api.Lifecycle;
using Gavel.Auctions.Api.Realtime;
using Gavel.Auctions.Application;
using Gavel.Auctions.Application.Abstractions;
using Gavel.Auctions.Infrastructure.Persistence;
using Gavel.Messaging;
using Gavel.Persistence;
using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Security;
using StackExchange.Redis;

namespace Gavel.Auctions.Api.Infrastructure;

internal static class AuctionsServiceRegistration
{
    public const string ServiceName = "auctions";
    public const string DatabaseName = "auctionsdb";
    public const string RedisName = "redis";
    public const int MaxHubMessageBytes = 4 * 1024;

    public static WebApplicationBuilder AddAuctionsService(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddGavelDbContext<AuctionsDbContext>(DatabaseName, (sp, options) => options.UseOutboxSignal(sp));
        services.AddScoped<IAuctionsDbContext>(sp => sp.GetRequiredService<AuctionsDbContext>());
        services.AddMessaging(ServiceName).AddOutbox<AuctionsDbContext>();
        services.AddGavelAuthentication();
        services.AddAuctionsApplication();

        services.AddOptions<LifecycleOptions>()
            .BindConfiguration(LifecycleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<AuctionLifecycleWorker>();

        var signalR = services
            .AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = MaxHubMessageBytes;
                options.EnableDetailedErrors = false;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IAuctionLiveUpdates, HubAuctionLiveUpdates>();

        var redis = builder.Configuration.GetConnectionString(RedisName);
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalR.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix = RedisChannel.Literal("gavel.auctions"));
            services.AddSingleton(_ => new RedisHealthCheck(redis));
            services.AddHealthChecks().AddCheck<RedisHealthCheck>(RedisName, tags: [ServiceDefaultsExtensions.ReadyTag]);
        }

        return builder;
    }
}
