using Gavel.Auctions.Api.Infrastructure;
using Gavel.Auctions.Api.Realtime;
using Gavel.Auctions.Application;
using Gavel.Auctions.Infrastructure.Persistence;
using Gavel.Persistence;
using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Endpoints;
using Gavel.Simulator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults(typeof(ApplicationServiceRegistration).Assembly);
builder.AddAuctionsService();
if (string.Equals(builder.Configuration["Simulator:InProcess"], "true", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddGavelSimulation();
}
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseApiDefaults();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference("/docs").AllowAnonymous();
}

app.MapDefaultEndpoints();
app.MapEndpointGroups(typeof(AuctionHub).Assembly);
app.MapHub<AuctionHub>(AuctionHub.Path);

await app.MigrateDatabaseAsync<AuctionsDbContext>();
await app.RunAsync();

public partial class Program;
