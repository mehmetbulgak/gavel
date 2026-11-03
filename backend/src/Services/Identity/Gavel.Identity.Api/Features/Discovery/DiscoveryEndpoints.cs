using Gavel.Identity.Api.Infrastructure.Security;
using Gavel.ServiceDefaults.Endpoints;
using Gavel.ServiceDefaults.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.Identity.Api.Features.Discovery;

internal sealed class DiscoveryEndpoints : IEndpointGroup
{
    public const string ConfigurationPath = "/.well-known/openid-configuration";
    public const string JwksPath = "/.well-known/jwks.json";

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/.well-known").AllowAnonymous().ExcludeFromDescription();

        group.MapGet("/openid-configuration", (HttpRequest request, IOptions<TokenValidationSettings> settings) => TypedResults.Ok(
            new Dictionary<string, object>
            {
                ["issuer"] = settings.Value.Issuer,
                ["jwks_uri"] = $"{request.Scheme}://{request.Host}{JwksPath}",
                ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.EcdsaSha256 },
            }));

        group.MapGet("/jwks.json", (SigningKeyProvider keys) => TypedResults.Ok(new
        {
            keys = new[]
            {
                new Dictionary<string, string>
                {
                    ["kty"] = "EC",
                    ["crv"] = "P-256",
                    ["x"] = keys.X,
                    ["y"] = keys.Y,
                    ["kid"] = keys.KeyId,
                    ["use"] = "sig",
                    ["alg"] = SecurityAlgorithms.EcdsaSha256,
                },
            },
        }));
    }
}
