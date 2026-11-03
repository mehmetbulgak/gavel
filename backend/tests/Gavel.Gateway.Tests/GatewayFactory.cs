using System.Net;
using Gavel.TestKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Gateway.Tests;

public sealed class GatewayFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const int SignInLimit = 3;
    public const int ApiLimit = 5;

    private readonly Dictionary<string, EchoService> _services = [];

    public async ValueTask InitializeAsync()
    {
        foreach (var name in new[] { "identity", "auctions", "notifications" })
        {
            _services[name] = await EchoService.StartAsync(name);
        }

        _ = Server;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var service in _services.Values)
        {
            await service.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public HttpClient CreateClientFrom(string clientAddress, string? token = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", clientAddress);
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth:MetadataAddress"] = string.Empty,
            ["ForwardedHeaders:Enabled"] = "true",
            ["RateLimits:SignIn"] = SignInLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["RateLimits:Api"] = ApiLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var (name, service) in _services)
        {
            settings[$"ReverseProxy:Clusters:{name}:Destinations:primary:Address"] = service.Address;
        }

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
        builder.ConfigureTestServices(services =>
        {
            services.AcceptTestTokens();
            services.AddSingleton<IStartupFilter, LoopbackConnectionFilter>();
        });
    }

    private sealed class LoopbackConnectionFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
