namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
