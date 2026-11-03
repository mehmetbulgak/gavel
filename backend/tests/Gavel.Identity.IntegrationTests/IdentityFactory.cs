using Gavel.TestKit;

namespace Gavel.Identity.IntegrationTests;

public class IdentityFactory : ServiceFactory<Program>, IAsyncLifetime
{
    protected override string DatabaseConnectionName => "identitydb";

    protected override string ServiceName => "identity";

    protected override bool AcceptsTestTokens => false;

    protected virtual int MaxLoginFailuresPerAccount => 1000;

    protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
    {
        ["LoginThrottling:MaxFailuresPerAccount"] = MaxLoginFailuresPerAccount.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    public async ValueTask InitializeAsync() => await StartAsync();

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}

public sealed class ThrottledIdentityFactory : IdentityFactory
{
    public const int MaxFailures = 3;

    protected override int MaxLoginFailuresPerAccount => MaxFailures;
}

[CollectionDefinition(Name)]
public sealed class IdentityCollection : ICollectionFixture<IdentityFactory>
{
    public const string Name = "identity";
}
