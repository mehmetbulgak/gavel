namespace Gavel.Identity.Api.Infrastructure.Security;

internal interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string? passwordHash, string password);
}
