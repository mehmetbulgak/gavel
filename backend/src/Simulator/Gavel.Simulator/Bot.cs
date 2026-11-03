using Gavel.Simulator.Api;

namespace Gavel.Simulator;

internal sealed class Bot(string email, string displayName, string password, bool isSeller)
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    private AuthResponse? _session;

    public string Email { get; } = email;

    public string DisplayName { get; } = displayName;

    public bool IsSeller { get; } = isSeller;

    public async Task<string> TokenAsync(GavelApi api, TimeProvider clock, CancellationToken ct)
    {
        if (_session is null || _session.ExpiresAt - clock.GetUtcNow() < RefreshMargin)
        {
            _session = await api.RegisterAsync(Email, DisplayName, password, ct) ?? await api.LoginAsync(Email, password, ct);
        }

        return _session.AccessToken;
    }

    public void Forget() => _session = null;
}
