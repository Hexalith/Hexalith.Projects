namespace Hexalith.Projects.Integration.Tests;

using Hexalith.Projects.E2E.Fixtures;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

/// <summary>Verifies the test observer cannot follow an ordinary/shared or unbound fixture graph.</summary>
public sealed class PersistedReadObserverBindingTests
{
    private const string Isolation = "reads-123456789abc";

    [Fact]
    public void OnlyExactPrivateRunTenantActorAndGraphBindingsCanBeObserved()
    {
        PersistedReadObserver observer = new(Configuration(true));
        LiveFixtureGraph graph = Graph();
        observer.CanObserve(graph).ShouldBeTrue();
        observer.CanObserve(graph with
        {
            RunId = "another-run"
        }).ShouldBeFalse();
        observer.CanObserve(graph with
        {
            TenantId = "shared-developer-tenant"
        }).ShouldBeFalse();
        observer.CanObserve(graph with
        {
            PrincipalId = "other-actor"
        }).ShouldBeFalse();
        observer.CanObserve(graph with
        {
            GraphId = "unregistered-graph"
        }).ShouldBeFalse();
    }

    [Fact]
    public void OrdinaryFixtureProfileHasNoObservationAuthority()
        => new PersistedReadObserver(Configuration(false)).CanObserve(Graph()).ShouldBeFalse();

    [Fact]
    public async Task SharedRedisDefaultIsRefusedBeforeAnyProviderProcessStarts()
    {
        PersistedReadObserver observer = new(Configuration(true));
        await Should.ThrowAsync<InvalidOperationException>(() => observer.ObserveAsync(Graph(), TestContext.Current.CancellationToken));
    }

    private static IConfiguration Configuration(bool enabled) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["FixtureStateStore:Enabled"] = enabled.ToString(),
        ["FixtureStateStore:IsolationId"] = Isolation,
        ["FixtureStateStore:Tenant"] = "owned-tenant",
        ["FixtureStateStore:PrimaryActor"] = "owned-actor",
        ["FixtureStateStore:ForeignTenant"] = "owned-foreign",
        ["FixtureStateStore:ForeignActor"] = "foreign-actor",
        ["FixtureStateStore:RedisHost"] = "127.0.0.1:6379",
    }).Build();

    private static LiveFixtureGraph Graph() => new(Isolation, Isolation, 0, 0, 0, "scope-check", "owned-tenant", "owned-actor",
        "p1", "p2", "proposal", "proposal-retry", "degraded", "conversation", "ambiguous", "existing", "stale", "forbidden",
        "unavailable", "folder", "folder2", "proposal-folder", "workspace", "file", "file2", "proposal-file", "denied-file",
        "fixture.txt", "memory", "correlation", "task", "idempotency");
}
