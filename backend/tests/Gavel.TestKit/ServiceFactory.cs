using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Gavel.TestKit;

public abstract class ServiceFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string _databaseName = $"gavel_test_{Guid.NewGuid():N}";
    private string _postgres = null!;
    private string _rabbitMq = null!;
    private string? _redis;

    public TestClock Clock { get; } = new();

    public string Exchange { get; } = $"gavel.test.{Guid.NewGuid():N}";

    public string RabbitMqConnectionString => _rabbitMq;

    public string DatabaseConnectionString => new NpgsqlConnectionStringBuilder(_postgres) { Database = _databaseName }.ConnectionString;

    protected abstract string DatabaseConnectionName { get; }

    protected abstract string ServiceName { get; }

    protected virtual bool UsesRedis => false;

    protected virtual bool AcceptsTestTokens => true;

    protected virtual IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>();

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public async Task StartAsync()
    {
        _postgres = await TestInfrastructure.PostgresAsync();
        _rabbitMq = await TestInfrastructure.RabbitMqAsync();
        _redis = UsesRedis ? await TestInfrastructure.RedisAsync() : null;
        _ = Server;
    }

    public async Task StopAsync()
    {
        await base.DisposeAsync();
        await DropDatabaseAsync();
        await RabbitMqTestTopology.DeleteAsync(_rabbitMq, Exchange, ServiceName);
    }

    public HttpClient CreateClientFor(Guid userId, string displayName = "Test User")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Issue(userId, displayName, DateTimeOffset.UtcNow));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{DatabaseConnectionName}"] = DatabaseConnectionString,
            ["ConnectionStrings:rabbitmq"] = _rabbitMq,
            ["Database:MigrateOnStartup"] = "true",
            ["Messaging:Exchange"] = Exchange,
            ["Messaging:OutboxPollingMilliseconds"] = "200",
            ["Auth:Issuer"] = TestTokens.Issuer,
            ["Auth:Audience"] = TestTokens.Audience,
        };

        if (_redis is not null)
        {
            settings["ConnectionStrings:redis"] = _redis;
        }

        foreach (var (key, value) in Settings)
        {
            settings[key] = value;
        }

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            if (AcceptsTestTokens)
            {
                services.AcceptTestTokens();
            }

            ConfigureServices(services);
        });
    }

    private async Task DropDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        var admin = new NpgsqlConnectionStringBuilder(_postgres) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var drop = connection.CreateCommand();
        drop.CommandText = string.Create(
            CultureInfo.InvariantCulture,
            $"DROP DATABASE IF EXISTS {new NpgsqlCommandBuilder().QuoteIdentifier(_databaseName)} WITH (FORCE)");
        await drop.ExecuteNonQueryAsync();
    }
}
