namespace Gavel.Identity.Api.Infrastructure.Security;

internal interface ILoginThrottle
{
    ValueTask<IDisposable> EnterAsync(string normalizedEmail, CancellationToken ct);

    bool IsBlocked(string normalizedEmail);

    void RecordFailure(string normalizedEmail);
}
