using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Gavel.Identity.Api.Infrastructure.Security;

internal sealed class LoginThrottle : ILoginThrottle, IDisposable
{
    private const int GateCount = 64;

    private readonly PartitionedRateLimiter<string> _failures;
    private readonly SemaphoreSlim[] _gates = Enumerable.Range(0, GateCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public LoginThrottle(IOptions<LoginThrottleOptions> options)
    {
        var limits = options.Value;
        _failures = PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.MaxFailuresPerAccount,
                Window = TimeSpan.FromMinutes(limits.WindowMinutes),
                QueueLimit = 0,
            }));
    }

    public async ValueTask<IDisposable> EnterAsync(string normalizedEmail, CancellationToken ct)
    {
        var gate = _gates[(uint)StringComparer.Ordinal.GetHashCode(normalizedEmail) % GateCount];
        await gate.WaitAsync(ct);
        return new Release(gate);
    }

    public bool IsBlocked(string normalizedEmail) =>
        _failures.GetStatistics(normalizedEmail) is { CurrentAvailablePermits: <= 0 };

    public void RecordFailure(string normalizedEmail)
    {
        using var lease = _failures.AttemptAcquire(normalizedEmail);
    }

    public void Dispose()
    {
        _failures.Dispose();
        foreach (var gate in _gates)
        {
            gate.Dispose();
        }
    }

    private sealed class Release(SemaphoreSlim gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
