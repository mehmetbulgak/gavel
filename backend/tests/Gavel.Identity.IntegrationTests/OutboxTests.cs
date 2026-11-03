using System.Data.Common;
using Gavel.Contracts.Identity;
using Gavel.Identity.Api.Infrastructure.Persistence;
using Gavel.TestKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Identity.IntegrationTests;

public sealed class OutboxQueryCounter : DbCommandInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("FROM outbox_messages", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _count);
        }

        return ValueTask.FromResult(result);
    }
}

public sealed class SlowPollingIdentityFactory : IdentityFactory
{
    public OutboxQueryCounter OutboxQueries { get; } = new();

    protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>(base.Settings)
    {
        ["Messaging:OutboxPollingMilliseconds"] = "60000",
    };

    protected override void ConfigureServices(IServiceCollection services) =>
        services.ConfigureDbContext<IdentityDbContext>(options => options.AddInterceptors(OutboxQueries));
}

public sealed class OutboxTests(SlowPollingIdentityFactory factory) : IClassFixture<SlowPollingIdentityFactory>
{
    private const int MaxIdleBatches = 2;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_event_wakes_the_publisher_without_waiting_for_the_polling_interval()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);

        var account = await Account.RegisterAsync(factory, displayName: "Prompt Pia");

        var published = await probe.WaitForAsync<UserRegistered>(e => e.UserId == account.Auth.User.Id, TimeSpan.FromSeconds(5));
        Assert.Equal("Prompt Pia", published.DisplayName);
    }

    [Fact]
    public async Task The_publisher_rests_while_there_is_nothing_to_send()
    {
        await using var probe = await EventProbe.StartAsync(factory.RabbitMqConnectionString, factory.Exchange);
        var account = await Account.RegisterAsync(factory, displayName: "Quiet Quinn");
        await probe.WaitForAsync<UserRegistered>(e => e.UserId == account.Auth.User.Id);
        await Task.Delay(TimeSpan.FromSeconds(1), Ct);

        var before = factory.OutboxQueries.Count;
        await Task.Delay(TimeSpan.FromSeconds(2), Ct);

        Assert.True(before > 0, "The publisher should have read the outbox at least once.");

        Assert.InRange(factory.OutboxQueries.Count - before, 0, MaxIdleBatches);
    }
}
