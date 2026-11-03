using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.ServiceDefaults.Security;

internal sealed class JwtBearerSetup(IOptions<TokenValidationSettings> settings, IHttpClientFactory httpClientFactory)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public const string MetadataClientName = "jwt-metadata";
    public const string HubPathPrefix = "/hubs";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            Configure(options);
        }
    }

    public void Configure(JwtBearerOptions options)
    {
        var validation = settings.Value;

        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.RequireHttpsMetadata = validation.RequireHttpsMetadata;
        if (!string.IsNullOrWhiteSpace(validation.MetadataAddress))
        {
            options.MetadataAddress = validation.MetadataAddress;
            options.Backchannel = httpClientFactory.CreateClient(MetadataClientName);
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = validation.Issuer,
            ValidAudience = validation.Audience,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = ClockSkew,
            NameClaimType = GavelClaims.Name,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var request = context.HttpContext.Request;
                if (request.Path.StartsWithSegments(HubPathPrefix, StringComparison.OrdinalIgnoreCase)
                    && request.Query.TryGetValue("access_token", out var token)
                    && !string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    }
}
