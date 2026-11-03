using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Gavel.TestKit;

public static class TestInfrastructure
{
    public const string PostgresVariable = "GAVEL_TEST_POSTGRES";
    public const string RabbitMqVariable = "GAVEL_TEST_RABBITMQ";
    public const string RedisVariable = "GAVEL_TEST_REDIS";

    private static readonly Lazy<Task<string>> Postgres = new(() => ResolveAsync(
        PostgresVariable,
        async () =>
        {
            var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await container.StartAsync();
            return container.GetConnectionString();
        }));

    private static readonly Lazy<Task<string>> RabbitMq = new(() => ResolveAsync(
        RabbitMqVariable,
        async () =>
        {
            var container = new RabbitMqBuilder("rabbitmq:4-alpine").Build();
            await container.StartAsync();
            return container.GetConnectionString();
        }));

    private static readonly Lazy<Task<string>> Redis = new(() => ResolveAsync(
        RedisVariable,
        async () =>
        {
            var container = new RedisBuilder("redis:7-alpine").Build();
            await container.StartAsync();
            return container.GetConnectionString();
        }));

    public static Task<string> PostgresAsync() => Postgres.Value;

    public static Task<string> RabbitMqAsync() => RabbitMq.Value;

    public static Task<string> RedisAsync() => Redis.Value;

    private static Task<string> ResolveAsync(string variable, Func<Task<string>> startContainer)
    {
        var configured = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(configured) ? startContainer() : Task.FromResult(configured);
    }
}
