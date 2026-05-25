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

    [Fact]
    public async Task ConcurrencyConflictShouldRetryWithinConfiguredAttempts()
    {
        FlakySaveStore store = new(new TenantAccessConcurrencyException("tenant-a", 0, 1));
        ProjectTenantAccessHandler handler = new(
            store,
            new FixedUtcClock(EventTimestamp.AddMinutes(1)),
            new TenantAccessOptions { ConcurrencyRetryAttempts = 2 });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.HandleAsync(Event(ProjectTenantAccessEventKind.TenantCreated, "tenant-a", "01J00000000000000000000080", 1), cancellationToken);

        ProjectTenantAccessProjection? projection = await store.GetAsync("tenant-a", cancellationToken);
        projection.ShouldNotBeNull();
        projection.Enabled.ShouldBeTrue();
        store.SaveAttempts.ShouldBe(2);
    }

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

    private sealed class FlakySaveStore(Exception firstSaveException) : IProjectTenantAccessProjectionStore
    {
        private readonly InMemoryProjectTenantAccessProjectionStore _inner = new();
        private bool _failed;

        public int SaveAttempts { get; private set; }

        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => _inner.GetAsync(tenantId, cancellationToken);

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (!_failed)
            {
                _failed = true;
                throw firstSaveException;
            }

            return _inner.SaveAsync(projection, cancellationToken);
        }
    }
}
