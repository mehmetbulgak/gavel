using Gavel.Auctions.Application.Lifecycle;
using Gavel.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Gavel.Auctions.IntegrationTests;

public sealed class AuctionsFactory : ServiceFactory<Program>, IAsyncLifetime
{
    protected override string DatabaseConnectionName => "auctionsdb";

    protected override string ServiceName => "auctions";

    protected override bool UsesRedis => true;

    public async ValueTask InitializeAsync() => await StartAsync();

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<int> RunLifecycleAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AuctionLifecycleProcessor>().RunOnceAsync(TestContext.Current.CancellationToken);
    }
}

[CollectionDefinition(Name)]
public sealed class AuctionsCollection : ICollectionFixture<AuctionsFactory>
{
    public const string Name = "auctions";
}
