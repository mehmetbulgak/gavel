using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Gateway.Tests;

public sealed class EchoService : IAsyncDisposable
{
    private readonly WebApplication _app;

    private EchoService(WebApplication app, string name, string address)
    {
        _app = app;
        Name = name;
        Address = address;
    }

    public string Name { get; }

    public string Address { get; }

    public static async Task<EchoService> StartAsync(string name)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.Run(context => context.Response.WriteAsJsonAsync(new EchoResponse(
            name,
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers["X-Forwarded-For"].ToString(),
            context.Request.Headers.Authorization.ToString())));
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new EchoService(app, name, address);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

public sealed record EchoResponse(string Service, string Method, string Path, string ForwardedFor, string Authorization);
