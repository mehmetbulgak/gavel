using Gavel.Notifications.Api.Infrastructure;
using Gavel.Notifications.Api.Infrastructure.Persistence;
using Gavel.Notifications.Api.Realtime;
using Gavel.Persistence;
using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults(typeof(Program).Assembly);
builder.AddNotificationsService();
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
app.MapHub<NotificationHub>(NotificationHub.Path, options => options.CloseOnAuthenticationExpiration = true);

await app.MigrateDatabaseAsync<NotificationsDbContext>();
await app.RunAsync();

public partial class Program;
