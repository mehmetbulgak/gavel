using Gavel.Gateway;
using Gavel.ServiceDefaults;
using Gavel.ServiceDefaults.Http;
using Gavel.ServiceDefaults.Security;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = ApiDefaultsExtensions.MaxRequestBodyBytes;
});

builder.Services.AddProblemDetails();
builder.Services.AddGavelTokenValidation();
builder.Services.AddAuthorization();
builder.Services.AddGatewayRateLimiting();
builder.Services.AddGatewayForwardedHeaders();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transforms =>
    {
        transforms.AddResponseHeaderRemove("Server", ResponseCondition.Always);
        transforms.AddResponseHeaderRemove("X-Powered-By", ResponseCondition.Always);
    })
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();
app.UseGatewayForwardedHeaders();
app.UseSecurityHeaders();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();

public partial class Program;
