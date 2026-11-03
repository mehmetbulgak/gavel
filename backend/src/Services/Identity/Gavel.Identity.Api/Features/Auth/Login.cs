using FluentValidation;
using Gavel.Identity.Api.Domain;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Features.Auth;

internal static class Login
{
    public sealed record Command(string Email, string Password);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Email).NotEmpty().MaximumLength(User.EmailMaxLength);
            RuleFor(c => c.Password).NotEmpty().MaximumLength(PasswordRules.MaxLength);
        }
    }

    public sealed class Handler(
        IdentityDbContext db,
        IPasswordHasher hasher,
        ILoginThrottle throttle,
        AuthSessionFactory sessions,
        TimeProvider clock)
    {
        public async Task<Result<AuthSession>> HandleAsync(Command command, CancellationToken ct)
        {
            var email = User.NormalizeEmail(command.Email);
            using var attempt = await throttle.EnterAsync(email, ct);
            if (throttle.IsBlocked(email))
            {
                return AuthErrors.TooManyFailedSignIns;
            }

            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
            var check = hasher.Verify(user?.PasswordHash, command.Password);
            if (user is null || check == PasswordCheck.Failed)
            {
                throttle.RecordFailure(email);
                return AuthErrors.InvalidCredentials;
            }

            if (check == PasswordCheck.SuccessRehashNeeded)
            {
                user.ChangePasswordHash(hasher.Hash(command.Password));
            }

            user.RecordLogin(clock.GetUtcNow());
            var session = sessions.Create(user);
            await db.SaveChangesAsync(ct);
            return session;
        }
    }
}
