using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Gavel.Auctions.Api.Infrastructure;

internal sealed class RedisHealthCheck(string connectionString) : IHealthCheck, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ConnectionMultiplexer? _connection;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken);
            await connection.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }

    private async Task<ConnectionMultiplexer> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync(ct);
        try
        {
            return _connection ??= await ConnectionMultiplexer.ConnectAsync(connectionString);
        }
        finally
        {
            _gate.Release();
        }
    }
}
