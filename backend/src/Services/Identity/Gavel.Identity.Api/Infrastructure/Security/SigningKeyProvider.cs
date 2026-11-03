using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class SigningKeyProvider : IDisposable
{
    private const int RequiredKeySize = 256;
    private const string PemMarker = "-----BEGIN";

    private readonly ECDsa _privateKey;
    private readonly ECDsa _publicKey;

    public SigningKeyProvider(IOptions<TokenIssuerOptions> options)
    {
        _privateKey = LoadOrGenerate(options.Value.SigningKey, out var ephemeral);
        IsEphemeral = ephemeral;

        if (_privateKey.KeySize != RequiredKeySize)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be an EC P-256 private key.");
        }

        var publicParameters = _privateKey.ExportParameters(includePrivateParameters: false);
        _publicKey = ECDsa.Create(publicParameters);
        X = Base64UrlEncoder.Encode(publicParameters.Q.X);
        Y = Base64UrlEncoder.Encode(publicParameters.Q.Y);
        KeyId = ComputeThumbprint(X, Y);

        SigningCredentials = new SigningCredentials(
            new ECDsaSecurityKey(_privateKey) { KeyId = KeyId },
            SecurityAlgorithms.EcdsaSha256);
        ValidationKey = new ECDsaSecurityKey(_publicKey) { KeyId = KeyId };
    }

    public bool IsEphemeral { get; }

    public string KeyId { get; }

    public string X { get; }

    public string Y { get; }

    public SigningCredentials SigningCredentials { get; }

    public SecurityKey ValidationKey { get; }

    public void Dispose()
    {
        _privateKey.Dispose();
        _publicKey.Dispose();
    }

    private static ECDsa LoadOrGenerate(string? configured, out bool ephemeral)
    {
        var key = ECDsa.Create();
        ephemeral = string.IsNullOrWhiteSpace(configured);

        if (ephemeral)
        {
            key.GenerateKey(ECCurve.NamedCurves.nistP256);
        }
        else if (configured!.Contains(PemMarker, StringComparison.Ordinal))
        {
            key.ImportFromPem(configured);
        }
        else
        {
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(configured.Trim()), out _);
        }

        return key;
    }

    private static string ComputeThumbprint(string x, string y)
    {
        var canonical = $$"""{"crv":"P-256","kty":"EC","x":"{{x}}","y":"{{y}}"}""";
        return Base64UrlEncoder.Encode(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
    }
}
