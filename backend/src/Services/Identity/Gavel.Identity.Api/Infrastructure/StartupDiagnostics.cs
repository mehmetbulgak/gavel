using Gavel.Identity.Api.Infrastructure.Security;

namespace Gavel.Identity.Api.Infrastructure;

internal sealed partial class StartupDiagnostics(SigningKeyProvider keys, ILogger<StartupDiagnostics> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (keys.IsEphemeral)
        {
            LogEphemeralKey(logger, keys.KeyId);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Jwt:SigningKey is not set. Generated a temporary ES256 key ({KeyId}); sessions end when this process restarts and other instances cannot share it.")]
    private static partial void LogEphemeralKey(ILogger logger, string keyId);
}
