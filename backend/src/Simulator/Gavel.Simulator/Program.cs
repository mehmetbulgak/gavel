using Gavel.ServiceDefaults;
namespace Gavel.Simulator;

internal static class SimulatorProgram
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddGavelSimulation();

        await builder.Build().RunAsync();
    }
}
