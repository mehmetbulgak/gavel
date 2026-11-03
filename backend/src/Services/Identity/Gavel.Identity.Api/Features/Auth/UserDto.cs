using Gavel.Identity.Api.Domain;

namespace Gavel.Identity.Api.Features.Auth;

internal sealed record UserDto(Guid Id, string Email, string DisplayName)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.DisplayName);
}
