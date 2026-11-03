namespace Gavel.Identity.Api.Features.Auth;

internal sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
