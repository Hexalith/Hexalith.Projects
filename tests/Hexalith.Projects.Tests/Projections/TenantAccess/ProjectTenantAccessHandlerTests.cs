// <copyright file="ProjectTenantAccessHandlerTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Projections.TenantAccess;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.TenantAccess;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tenant-access projection tests for the Story 1.6 local Tenants-event read model.
/// </summary>
public sealed class ProjectTenantAccessHandlerTests
{
    private static readonly DateTimeOffset EventTimestamp = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UserAddedToTenantShouldProjectMetadataOnlyAccessEvidence()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", "01J00000000000000000000001", 1), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserAddedToTenant, "tenant-a", "01J00000000000000000000002", 2, principalId: "user-a", role: "TenantOwner"), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);

        projection.ShouldNotBeNull();
        projection.Enabled.ShouldBeTrue();
        projection.Watermark.ShouldBe(2);
        projection.ProjectionWatermark.ShouldBe("tenant-a:2");
        projection.Principals["user-a"].Role.ShouldBe("TenantOwner");
    }

    [Fact]
    public async Task DuplicateMessageWithDivergentMetadataShouldRecordReplayConflictWithoutAdvancingWatermark()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        string messageId = "01J00000000000000000000010";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", messageId, 1, payloadFingerprint: "created"), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantDisabled, "tenant-a", messageId, 2, payloadFingerprint: "disabled"), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);

        projection.ShouldNotBeNull();
        projection.Watermark.ShouldBe(1);
        projection.ReplayConflict.ShouldBeTrue();
        projection.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task OutOfOrderDeliveryShouldNotAdvanceProjectionOrMarkMalformed()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", "01J00000000000000000000050", 2), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantDisabled, "tenant-a", "01J00000000000000000000049", 1), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);

        projection.ShouldNotBeNull();
        projection.Enabled.ShouldBeTrue();
        projection.Watermark.ShouldBe(2);
        projection.MalformedEvidence.ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectConfigurationKeysShouldBeScopedAndTombstoned()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", "01J00000000000000000000020", 1), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantConfigurationSet, "tenant-a", "01J00000000000000000000021", 2, configurationKey: "billing.plan"), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantConfigurationSet, "tenant-a", "01J00000000000000000000022", 3, configurationKey: "projects.create.enabled"), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantConfigurationRemoved, "tenant-a", "01J00000000000000000000023", 4, configurationKey: "projects.create.enabled"), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);

        projection.ShouldNotBeNull();
        projection.ConfigurationKeys.ShouldNotContain("billing.plan");
        projection.ConfigurationKeys.ShouldNotContain("projects.create.enabled");
        projection.RemovedConfigurationKeys.ShouldContain("projects.create.enabled");
    }

    [Fact]
    public async Task MalformedEvidenceShouldFailClosed()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", string.Empty, 1), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserAddedToTenant, "tenant-b", "01J00000000000000000000060", 1, role: "TenantReader"), cancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-c", "01J00000000000000000000061", 1, timestamp: EventTimestamp.AddMinutes(5)), cancellationToken);

        (await store.GetAsync("tenant-a", cancellationToken)).ShouldNotBeNull().MalformedEvidence.ShouldBeTrue();
        (await store.GetAsync("tenant-b", cancellationToken)).ShouldNotBeNull().MalformedEvidence.ShouldBeTrue();
        (await store.GetAsync("tenant-c", cancellationToken)).ShouldNotBeNull().MalformedEvidence.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshRestoresMissingProjectionWithExactStatusRolesAndKeys(bool enabled)
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        ProjectTenantAccessEvent refresh = Refresh("recovery", 50, enabled);
        await handler.HandleAsync(refresh, TestContext.Current.CancellationToken);

        ProjectTenantAccessProjection projection = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        projection.Enabled.ShouldBe(enabled);
        projection.Principals.Keys.ShouldBe(["owner", "reader"]);
        projection.Principals["owner"].Role.ShouldBe("TenantOwner");
        projection.Principals["reader"].Role.ShouldBe("TenantReader");
        projection.ConfigurationKeys.ShouldBe(["projects.allowed"]);
        projection.Watermark.ShouldBe(50);
        projection.SnapshotWatermark.ShouldBe(50);
        projection.LastEventTimestamp.ShouldBe(EventTimestamp);
        projection.ProcessedMessages["recovery"].PayloadFingerprint.ShouldBe(refresh.PayloadFingerprint);
        TenantAccessAuthorizer authorizer = new(store, new FixedUtcClock(EventTimestamp.AddMinutes(1)), new TenantAccessOptions());
        TenantAccessAuthorizationResult authorization = await authorizer.AuthorizeMutationAsync(new("tenant-a", "owner", "tenant-a"), TestContext.Current.CancellationToken);
        authorization.Outcome.ShouldBe(enabled ? TenantAccessOutcome.Allowed : TenantAccessOutcome.DisabledTenant);
    }

    [Fact]
    public async Task RefreshIgnoresUnrelatedWhitespaceConfigurationKeysAcceptedByTenants()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        ProjectTenantAccessEvent refresh = new ProjectsTenantAccessEventMapper().Map(
            ProjectTenantAccessEventKind.TenantAccessRefreshed, "tenant-a", "system", "recovery-whitespace", 50,
            EventTimestamp, "correlation", snapshot: new(true, [new("owner", "TenantOwner")], ["projects.allowed", " ", "\t"]));

        await handler.HandleAsync(refresh, TestContext.Current.CancellationToken);

        ProjectTenantAccessProjection projection = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        projection.MalformedEvidence.ShouldBeFalse();
        projection.ConfigurationKeys.ShouldBe(["projects.allowed"]);
        projection.Principals["owner"].Role.ShouldBe("TenantOwner");
        projection.Watermark.ShouldBe(50);
    }

    [Fact]
    public async Task RefreshReplacesStaleRolesObsoletePrincipalsTombstonesAndConflictFlags()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessProjection stale = new() { TenantId = "tenant-a", Enabled = true, Watermark = 10,
            LastEventTimestamp = EventTimestamp.AddDays(-1), MalformedEvidence = true, ReplayConflict = true };
        stale.Principals["obsolete"] = new("obsolete", "TenantOwner");
        stale.Principals["reader"] = new("reader", "TenantOwner");
        stale.ConfigurationKeys.Add("projects.obsolete");
        stale.RemovedConfigurationKeys.Add("projects.allowed");
        await store.SaveAsync(stale, TestContext.Current.CancellationToken);
        await CreateHandler(store, EventTimestamp.AddMinutes(1)).HandleAsync(Refresh("fresh-recovery", 50, true), TestContext.Current.CancellationToken);

        ProjectTenantAccessProjection projection = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        projection.Principals.ShouldNotContainKey("obsolete");
        projection.Principals["reader"].Role.ShouldBe("TenantReader");
        projection.ConfigurationKeys.ShouldBe(["projects.allowed"]);
        projection.RemovedConfigurationKeys.ShouldBeEmpty();
        projection.MalformedEvidence.ShouldBeFalse();
        projection.ReplayConflict.ShouldBeFalse();
    }

    [Fact]
    public async Task DelayedRefreshNeverOverwritesLaterRevocationAndFreshOperationRepairsOvertakenRecovery()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserRemovedFromTenant, "tenant-a", "revoke", 51, principalId: "reader"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(Refresh("overtaken", 50, true), TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection incomplete = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        incomplete.MalformedEvidence.ShouldBeTrue();
        incomplete.Watermark.ShouldBe(51);
        incomplete.Principals.ShouldNotContainKey("reader");

        ProjectTenantAccessEvent next = new ProjectsTenantAccessEventMapper().Map(ProjectTenantAccessEventKind.TenantAccessRefreshed,
            "tenant-a", "system", "later-operation", 52, EventTimestamp, "correlation", snapshot: new(true, [new("owner", "TenantOwner")], []));
        await handler.HandleAsync(next, TestContext.Current.CancellationToken);
        await handler.HandleAsync(Refresh("overtaken", 50, true), TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection recovered = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        recovered.Watermark.ShouldBe(52);
        recovered.MalformedEvidence.ShouldBeFalse();
        recovered.Principals.Keys.ShouldBe(["owner"]);
    }

    [Fact]
    public async Task DuplicateAndConflictingSnapshotsCannotRegrantAfterDisableOrRemoval()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        ProjectTenantAccessEvent refresh = Refresh("recovery", 50, true);
        await handler.HandleAsync(refresh, TestContext.Current.CancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserRemovedFromTenant, "tenant-a", "remove", 51, principalId: "reader"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantDisabled, "tenant-a", "disable", 52), TestContext.Current.CancellationToken);
        await handler.HandleAsync(refresh, TestContext.Current.CancellationToken);
        await handler.HandleAsync(refresh with { PayloadFingerprint = "divergent" }, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection projection = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        projection.Enabled.ShouldBeFalse();
        projection.Principals.ShouldNotContainKey("reader");
        projection.Watermark.ShouldBe(52);
        projection.ReplayConflict.ShouldBeTrue();
    }

    [Fact]
    public async Task ForwardGapAndUnseenLateRemovalDenyUntilAFreshCompleteSnapshot()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        TenantAccessAuthorizer authorizer = new(store, new FixedUtcClock(EventTimestamp.AddMinutes(1)), new TenantAccessOptions());
        await handler.HandleAsync(Refresh("snapshot-50", 50, true), TestContext.Current.CancellationToken);
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "reader", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantUpdated, "tenant-a", "unrelated-52", 52), TestContext.Current.CancellationToken);
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "reader", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserRemovedFromTenant, "tenant-a", "removal-51", 51, principalId: "reader"), TestContext.Current.CancellationToken);
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "reader", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
        ProjectTenantAccessProjection incomplete = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        incomplete.Watermark.ShouldBe(52);
        incomplete.MalformedEvidence.ShouldBeTrue();

        ProjectTenantAccessEvent complete = new ProjectsTenantAccessEventMapper().Map(ProjectTenantAccessEventKind.TenantAccessRefreshed,
            "tenant-a", "system", "snapshot-53", 53, EventTimestamp, "correlation", snapshot: new(true, [new("owner", "TenantOwner")], []));
        await handler.HandleAsync(complete, TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection recovered = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        recovered.SnapshotWatermark.ShouldBe(53);
        recovered.MalformedEvidence.ShouldBeFalse();
        recovered.Principals.Keys.ShouldBe(["owner"]);
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "owner", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
        (await authorizer.AuthorizeMutationAsync(new("tenant-a", "reader", "tenant-a"), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task DeltasAlreadyCoveredByTheSnapshotCannotPoisonOrChangeItsEvidence()
    {
        InMemoryProjectTenantAccessProjectionStore store = new();
        ProjectTenantAccessHandler handler = CreateHandler(store, EventTimestamp.AddMinutes(1));
        await handler.HandleAsync(Refresh("snapshot", 50, true), TestContext.Current.CancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.UserRemovedFromTenant, "tenant-a", "old-removal", 49, principalId: "owner"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantDisabled, "tenant-a", "covered", 50), TestContext.Current.CancellationToken);
        ProjectTenantAccessProjection projection = (await store.GetAsync("tenant-a", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        projection.Enabled.ShouldBeTrue();
        projection.Principals.ShouldContainKey("owner");
        projection.MalformedEvidence.ShouldBeFalse();
        projection.ReplayConflict.ShouldBeFalse();
        projection.Version.ShouldBe(1);
    }

    private static ProjectTenantAccessEvent Refresh(string messageId, long sequence, bool enabled)
        => new ProjectsTenantAccessEventMapper().Map(ProjectTenantAccessEventKind.TenantAccessRefreshed,
            "tenant-a", "system", messageId, sequence, EventTimestamp, "correlation",
            snapshot: new(enabled, [new("owner", "TenantOwner"), new("reader", "TenantReader")], ["projects.allowed", "billing.plan"]));

    private static ProjectTenantAccessHandler CreateHandler(IProjectTenantAccessProjectionStore store, DateTimeOffset now)
        => new(store, new FixedUtcClock(now), new TenantAccessOptions());

    private static ProjectTenantAccessEvent Event(
        ProjectTenantAccessEventKind kind,
        string tenantId,
        string messageId,
        long sequenceNumber,
        string? principalId = null,
        string? role = null,
        string? configurationKey = null,
        string? payloadFingerprint = null,
        DateTimeOffset? timestamp = null)
        => new(
            kind,
            tenantId,
            messageId,
            sequenceNumber,
            timestamp ?? EventTimestamp,
            "correlation-a",
            principalId,
            role,
            ConfigurationKey: configurationKey,
            PayloadFingerprint: payloadFingerprint ?? tenantId);

}
