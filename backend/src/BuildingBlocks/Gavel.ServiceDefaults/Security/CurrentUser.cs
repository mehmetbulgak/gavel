using System.Security.Claims;
using Gavel.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Gavel.ServiceDefaults.Security;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id => IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue(GavelClaims.Subject), out var id) ? id : null;

    public string? DisplayName => IsAuthenticated ? Principal!.FindFirstValue(GavelClaims.Name) : null;
}
