namespace Gavel.Simulator.Api;

internal sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUser User);
