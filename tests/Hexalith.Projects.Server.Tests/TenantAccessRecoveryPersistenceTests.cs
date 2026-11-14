namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;

using Shouldly;
using Xunit;

/// <summary>Verifies conditional first creation and retries through the real durable tenant-access adapter.</summary>
public sealed class TenantAccessRecoveryPersistenceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task MissingProjectionInsideExistingDocumentRecoversAndPreservesLogicalVersion()
    {
        InMemoryReadModelStore backend = new();
        backend.SeedRaw("statestore", "projects:tenant-access:tenant-a", new { Projection = (object?)null, Version = 7L });
        (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldBeNull();
        await Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(Snapshot("recovery", 50, true), TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection saved = (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        saved.Watermark.ShouldBe(50);
        saved.Version.ShouldBe(8);
        saved.Principals.Keys.ShouldBe(["owner", "reader"]);
        saved.MalformedEvidence.ShouldBeFalse();
    }

    [Fact]
    public async Task CompetingSnapshotFirstCreatesConvergeToOneCompletePersistedResultAcrossStoreRecreation()
    {
        InMemoryReadModelStore backend = new();
        ProjectTenantAccessEvent refresh = Snapshot("operation-event", 50, true);
        backend.ConcurrentWriteBeforeTrySave = () =>
        {
            backend.ConcurrentWriteBeforeTrySave = null;
            Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(refresh, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        };

        await Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(refresh, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection saved = (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        saved.Enabled.ShouldBeTrue();
        saved.Watermark.ShouldBe(50);
        saved.SnapshotWatermark.ShouldBe(50);
        saved.Version.ShouldBe(1);
        saved.Principals.Keys.ShouldBe(["owner", "reader"]);
        saved.ConfigurationKeys.ShouldBe(["projects.allowed"]);
        saved.ProcessedMessages.ShouldHaveSingleItem().Key.ShouldBe("operation-event");
    }

    [Theory]
    [InlineData(ProjectTenantAccessEventKind.TenantDisabled)]
    [InlineData(ProjectTenantAccessEventKind.UserRemovedFromTenant)]
    public async Task ConcurrentLiveFirstCreateCannotBeOverwrittenByAnOlderSnapshot(ProjectTenantAccessEventKind liveKind)
    {
        InMemoryReadModelStore backend = new();
        backend.ConcurrentWriteBeforeTrySave = () =>
        {
            backend.ConcurrentWriteBeforeTrySave = null;
            ProjectTenantAccessEvent live = new(liveKind, "tenant-a", "live-event", 51, Now, "correlation", PrincipalId: "reader");
            Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(live, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        };

        await Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(Snapshot("overtaken", 50, true), TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection saved = (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        saved.Watermark.ShouldBe(51);
        saved.MalformedEvidence.ShouldBeTrue();
        saved.Principals.ShouldNotContainKey("reader");

        ProjectTenantAccessEvent later = new ProjectsTenantAccessEventMapper().Map(ProjectTenantAccessEventKind.TenantAccessRefreshed,
            "tenant-a", "system", "new-operation-event", 52, Now, "correlation",
            snapshot: new(liveKind != ProjectTenantAccessEventKind.TenantDisabled, [new("owner", "TenantOwner")], []));
        await Handler(new DaprProjectTenantAccessProjectionStore(backend)).HandleAsync(later, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection recovered = (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        recovered.Watermark.ShouldBe(52);
        recovered.MalformedEvidence.ShouldBeFalse();
        recovered.Enabled.ShouldBe(liveKind != ProjectTenantAccessEventKind.TenantDisabled);
        recovered.Principals.Keys.ShouldBe(["owner"]);
    }

    [Fact]
    public async Task ConflictExhaustionIsVisibleAndLeavesNoPartiallyCreatedAuthorization()
    {
        InMemoryReadModelStore backend = new();
        ConflictingTenantAccessReadModelStore conflicts = new(backend);
        await Should.ThrowAsync<InvalidOperationException>(() => Handler(new DaprProjectTenantAccessProjectionStore(conflicts))
            .HandleAsync(Snapshot("conflict", 50, true), TestContext.Current.CancellationToken));
        conflicts.Attempts.ShouldBe(3);
        (await new DaprProjectTenantAccessProjectionStore(backend).GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    private static ProjectTenantAccessHandler Handler(IProjectTenantAccessProjectionStore store)
        => new(store, new FixedUtcClock(Now), new TenantAccessOptions { ConcurrencyRetryAttempts = 3 });

    private static ProjectTenantAccessEvent Snapshot(string messageId, long sequence, bool enabled)
        => new ProjectsTenantAccessEventMapper().Map(ProjectTenantAccessEventKind.TenantAccessRefreshed,
            "tenant-a", "system", messageId, sequence, Now, "correlation",
            snapshot: new(enabled, [new("owner", "TenantOwner"), new("reader", "TenantReader")], ["projects.allowed"]));
}
