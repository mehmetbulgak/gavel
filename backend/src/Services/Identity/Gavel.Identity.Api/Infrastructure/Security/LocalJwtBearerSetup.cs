using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class LocalJwtBearerSetup(SigningKeyProvider keys) : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        options.TokenValidationParameters.IssuerSigningKey = keys.ValidationKey;
    }
}
