using System.Text.RegularExpressions;
using Gavel.SharedKernel;

namespace Gavel.Identity.Api.Domain;

internal sealed partial class User : Entity
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 40;

    private User()
    {
    }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static bool IsValidDisplayName(string displayName) => DisplayNamePattern().IsMatch(displayName.Trim());

    public static Result<User> Create(string email, string displayName, string passwordHash, DateTimeOffset now)
    {
        email = NormalizeEmail(email);
        displayName = displayName.Trim();

        if (email.Length is 0 or > EmailMaxLength || !email.Contains('@', StringComparison.Ordinal))
        {
            return Error.Validation("User.InvalidEmail", "A valid email address is required.");
        }

        if (!IsValidDisplayName(displayName))
        {
            return Error.Validation(
                "User.InvalidDisplayName",
                $"Display name must be {DisplayNameMinLength}-{DisplayNameMaxLength} letters, digits, spaces or . _ ' -");
        }

        return new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            CreatedAt = now,
            LastLoginAt = now,
        };
    }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void RecordLogin(DateTimeOffset now) => LastLoginAt = now;

    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{N} ._'-]{1,39}$")]
    private static partial Regex DisplayNamePattern();
}
