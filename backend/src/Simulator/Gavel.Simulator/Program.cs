using Gavel.ServiceDefaults;
using Gavel.Simulator;
using Gavel.Simulator.Api;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOptions<SimulatorOptions>()
    .BindConfiguration(SimulatorOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<GavelApi>((services, http) =>
    http.BaseAddress = new Uri(services.GetRequiredService<IOptions<SimulatorOptions>>().Value.GatewayUrl));
builder.Services.AddHostedService<SimulationWorker>();

await builder.Build().RunAsync();
