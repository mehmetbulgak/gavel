using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace Gavel.Gateway;

internal static class ForwardedHeadersSetup
{
    public static IServiceCollection AddGatewayForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersSettings>()
            .BindConfiguration(ForwardedHeadersSettings.SectionName)
            .Validate(
                settings => settings.KnownNetworks.All(network => IPNetwork.TryParse(network, out _)),
                "ForwardedHeaders:KnownNetworks must contain CIDR ranges such as 172.16.0.0/12.")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                foreach (var network in settings.Value.KnownNetworks)
                {
                    options.KnownIPNetworks.Add(IPNetwork.Parse(network));
                }
            });

        return services;
    }

    public static WebApplication UseGatewayForwardedHeaders(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value.Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
