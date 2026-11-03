using Gavel.ServiceDefaults.Endpoints;
using Gavel.ServiceDefaults.Http;

namespace Gavel.Identity.Api.Features.Auth;

internal sealed class AuthEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithValidation<Register.Command>()
            .Produces<AuthResponse>(StatusCodes.Status201Created);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithValidation<Login.Command>()
            .Produces<AuthResponse>();

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .Produces<AuthResponse>();

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetMeAsync)
            .Produces<UserDto>();
    }

    private static async Task<IResult> RegisterAsync(Register.Command command, Register.Handler handler, HttpContext http, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(session => SignIn(http, session, created: true));

    private static async Task<IResult> LoginAsync(Login.Command command, Login.Handler handler, HttpContext http, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(session => SignIn(http, session));

    private static async Task<IResult> RefreshAsync(RefreshSession.Handler handler, HttpContext http, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RefreshSession.Command(RefreshTokenCookie.Read(http.Request)), ct);
        if (result.IsFailure && result.Error != AuthErrors.RefreshTokenAlreadyRotated)
        {
            RefreshTokenCookie.Clear(http.Response);
        }

        return result.ToHttpResult(session => SignIn(http, session));
    }

    private static async Task<IResult> LogoutAsync(Logout.Handler handler, HttpContext http, CancellationToken ct)
    {
        await handler.HandleAsync(new Logout.Command(RefreshTokenCookie.Read(http.Request)), ct);
        RefreshTokenCookie.Clear(http.Response);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetMeAsync(GetCurrentUser.Handler handler, CancellationToken ct) =>
        (await handler.HandleAsync(ct)).ToHttpResult();

    private static IResult SignIn(HttpContext http, AuthSession session, bool created = false)
    {
        RefreshTokenCookie.Write(http.Response, session.RefreshToken, session.RefreshTokenExpiresAt);
        var body = new AuthResponse(session.AccessToken.Token, session.AccessToken.ExpiresAt, session.User);
        return created ? TypedResults.Created("/api/auth/me", body) : TypedResults.Ok(body);
    }
}
