namespace Gavel.Identity.Api.Features.Auth;

internal static class RefreshTokenCookie
{
    public const string Name = "__Secure-gavel_rt";
    public const string Path = "/api/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Write(HttpResponse response, string token, DateTimeOffset expires) =>
        response.Cookies.Append(Name, token, Options(expires));

    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name, Options(expires: null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
