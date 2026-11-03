using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Identity.Api.Features.Auth;

internal static class GetCurrentUser
{
    public sealed class Handler(IdentityDbContext db, ICurrentUser currentUser)
    {
        public async Task<Result<UserDto>> HandleAsync(CancellationToken ct)
        {
            var userId = currentUser.RequiredId;
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct);
            return user is null ? AuthErrors.UserNotFound : UserDto.From(user);
        }
    }
}
