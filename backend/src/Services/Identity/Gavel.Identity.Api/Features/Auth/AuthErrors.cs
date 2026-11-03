using Gavel.SharedKernel;

namespace Gavel.Identity.Api.Features.Auth;

internal static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Your session has expired. Please sign in again.");

    public static readonly Error RefreshTokenAlreadyRotated = Error.Unauthorized(
        "Auth.RefreshTokenRotated",
        "This session was refreshed by another request a moment ago. Retry with the new session.");

    public static readonly Error TooManyFailedSignIns = Error.TooManyRequests(
        "Auth.TooManyFailedSignIns",
        "Too many failed sign-in attempts for this account. Please wait a few minutes and try again.");

    public static readonly Error EmailTaken = Error.Conflict("Auth.EmailTaken", "An account with this email already exists.");

    public static readonly Error UserNotFound = Error.NotFound("Auth.UserNotFound", "User not found.");
}
