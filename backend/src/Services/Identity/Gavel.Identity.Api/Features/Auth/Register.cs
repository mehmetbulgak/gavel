using FluentValidation;
using Gavel.Contracts.Identity;
using Gavel.Identity.Api.Domain;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.Identity.Api.Infrastructure.Security;
using Gavel.Messaging.Outbox;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Features.Auth;

internal static class Register
{
    public sealed record Command(string Email, string DisplayName, string Password);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
            RuleFor(c => c.DisplayName)
                .NotEmpty()
                .Must(User.IsValidDisplayName)
                .WithMessage($"Display name must be {User.DisplayNameMinLength}-{User.DisplayNameMaxLength} letters, digits, spaces or . _ ' -");
            RuleFor(c => c.Password).ValidPassword();
        }
    }

    public sealed class Handler(IdentityDbContext db, IPasswordHasher hasher, AuthSessionFactory sessions, TimeProvider clock)
    {
        public async Task<Result<AuthSession>> HandleAsync(Command command, CancellationToken ct)
        {
            var email = User.NormalizeEmail(command.Email);
            if (await db.Users.AnyAsync(u => u.Email == email, ct))
            {
                return AuthErrors.EmailTaken;
            }

            var now = clock.GetUtcNow();
            var user = User.Create(email, command.DisplayName, hasher.Hash(command.Password), now);
            if (user.IsFailure)
            {
                return user.Error!;
            }

            db.Users.Add(user.Value);
            db.AddIntegrationEvent(new UserRegistered(user.Value.Id, user.Value.Email, user.Value.DisplayName), now);
            var session = sessions.Create(user.Value);
            await db.SaveChangesAsync(ct);
            return session;
        }
    }
}
