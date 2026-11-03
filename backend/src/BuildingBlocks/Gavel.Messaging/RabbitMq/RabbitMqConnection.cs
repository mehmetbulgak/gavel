using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Gavel.Messaging.RabbitMq;

public sealed class RabbitMqConnection(IConfiguration configuration, IOptions<MessagingOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await CreateFactory().CreateConnectionAsync(options.Value.ServiceName, ct);
            return _connection;
        }
        finally
        {
            _gate.Release();
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

    private ConnectionFactory CreateFactory()
    {
        var connectionString = configuration.GetConnectionString(MessagingOptions.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{MessagingOptions.ConnectionStringName}' is not configured.");

        return new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };
    }
}
