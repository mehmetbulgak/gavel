using Gavel.Identity.Api.Features.Auth;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;
using Gavel.Messaging;
using Gavel.Persistence;
using Gavel.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Gavel.Identity.Api.Infrastructure;

internal static class IdentityServiceRegistration
{
    public const string ServiceName = "identity";
    public const string DatabaseName = "identitydb";

    public static WebApplicationBuilder AddIdentityService(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddGavelDbContext<IdentityDbContext>(DatabaseName, (sp, options) => options.UseOutboxSignal(sp));
        services.AddMessaging(ServiceName).AddOutbox<IdentityDbContext>();

        services.AddOptions<TokenIssuerOptions>()
            .BindConfiguration(TokenIssuerOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<LoginThrottleOptions>()
            .BindConfiguration(LoginThrottleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<SigningKeyProvider>();
        services.AddSingleton<ITokenIssuer, TokenIssuer>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ILoginThrottle, LoginThrottle>();
        services.AddGavelAuthentication();
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, LocalJwtBearerSetup>();
        services.AddHostedService<StartupDiagnostics>();

        services.AddScoped<AuthSessionFactory>();
        services.AddScoped<Register.Handler>();
        services.AddScoped<Login.Handler>();
        services.AddScoped<RefreshSession.Handler>();
        services.AddScoped<Logout.Handler>();
        services.AddScoped<GetCurrentUser.Handler>();

        return builder;
    }
}
