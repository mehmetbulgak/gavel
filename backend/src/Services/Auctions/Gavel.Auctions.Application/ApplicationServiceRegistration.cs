using Gavel.Auctions.Application.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Auctions.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddAuctionsApplication(this IServiceCollection services)
    {
        var handlers = typeof(ApplicationServiceRegistration).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: true, Name: "Handler" });

        foreach (var handler in handlers)
        {
            services.AddScoped(handler);
        }

        services.AddScoped<AuctionLifecycleProcessor>();
        return services;
    }
}
