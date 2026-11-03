using Microsoft.AspNetCore.SignalR;

namespace Gavel.ServiceDefaults.Security;

internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst(GavelClaims.Subject)?.Value;
}
