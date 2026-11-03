using Gavel.Identity.Api.Infrastructure;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Persistence;
using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults(typeof(Program).Assembly);
builder.AddIdentityService();
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
app.MapEndpointGroups(typeof(Program).Assembly);

await app.MigrateDatabaseAsync<IdentityDbContext>();
await app.RunAsync();

public partial class Program;
