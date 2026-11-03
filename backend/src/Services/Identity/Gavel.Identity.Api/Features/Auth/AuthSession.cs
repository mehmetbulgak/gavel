using Gavel.Identity.Api.Infrastructure.Security;

namespace Gavel.Identity.Api.Features.Auth;

internal sealed record AuthSession(AccessToken AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, UserDto User);
