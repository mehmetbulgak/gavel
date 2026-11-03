using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    public const int IterationCount = 210_000;

    private static readonly object HashSubject = new();

    private readonly PasswordHasher<object> _inner = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = IterationCount,
    }));

    private readonly Lazy<string> _decoyHash;

    public PasswordHasher() => _decoyHash = new Lazy<string>(() => Hash(Guid.NewGuid().ToString()));

    public string Hash(string password) => _inner.HashPassword(HashSubject, password);

    public PasswordCheck Verify(string? passwordHash, string password)
    {
        if (passwordHash is null)
        {
            _inner.VerifyHashedPassword(HashSubject, _decoyHash.Value, password);
            return PasswordCheck.Failed;
        }

        return _inner.VerifyHashedPassword(HashSubject, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed,
        };
    }
}
