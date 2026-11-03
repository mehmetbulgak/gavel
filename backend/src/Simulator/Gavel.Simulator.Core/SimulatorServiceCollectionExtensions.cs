using Gavel.Simulator.Api;
using Microsoft.Extensions.Options;

namespace Gavel.Simulator;

public static class SimulatorServiceCollectionExtensions
{
    public static IServiceCollection AddGavelSimulation(this IServiceCollection services)
    {
        services.AddOptions<SimulatorOptions>()
            .BindConfiguration(SimulatorOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<GavelApi>((provider, http) =>
            http.BaseAddress = new Uri(provider.GetRequiredService<IOptions<SimulatorOptions>>().Value.GatewayUrl));
        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
