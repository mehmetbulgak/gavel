namespace Gavel.Identity.Api.Infrastructure.Security;

internal enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded,
}
