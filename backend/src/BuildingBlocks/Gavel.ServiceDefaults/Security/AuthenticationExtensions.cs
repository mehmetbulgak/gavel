using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gavel.ServiceDefaults.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddGavelAuthentication(this IServiceCollection services)
    {
        services.AddGavelTokenValidation();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

        return services;
    }

    public static IServiceCollection AddGavelTokenValidation(this IServiceCollection services)
    {
        services.AddOptions<TokenValidationSettings>()
            .BindConfiguration(TokenValidationSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient(JwtBearerSetup.MetadataClientName);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerSetup>();

        return services;
    }
}
