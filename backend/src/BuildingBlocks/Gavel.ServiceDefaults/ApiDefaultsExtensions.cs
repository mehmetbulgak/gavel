using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using Gavel.ServiceDefaults.Http;
using Gavel.ServiceDefaults.Security;
using Gavel.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.ServiceDefaults;

public static class ApiDefaultsExtensions
{
    public const long MaxRequestBodyBytes = 64 * 1024;

    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder, Assembly applicationAssembly)
    {
        builder.AddServiceDefaults();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.AddServerHeader = false;
            kestrel.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
        });

        var services = builder.Services;
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.RespectNullableAnnotations = true;
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        return builder;
    }

    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        app.UseSecurityHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
