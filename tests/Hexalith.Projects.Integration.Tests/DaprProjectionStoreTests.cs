// <copyright file="DaprProjectionStoreTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Collections.Concurrent;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.TenantAccess;

using Shouldly;

using Xunit;

/// <summary>
/// Durable projection-store tests using a fake state backend; no live Dapr sidecar is required.
/// </summary>
public sealed class DaprProjectionStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies tenant-access store preserves optimistic concurrency and detached snapshots.</summary>
    [Fact]
    public async Task TenantAccessStoreShouldPersistDetachedProjectionWithOptimisticConcurrency()
    {
        FakeProjectsStateStore state = new();
        DaprProjectTenantAccessProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ProjectTenantAccessProjection projection = new() { TenantId = "tenant-a", Enabled = true };
        await store.SaveAsync(projection, cancellationToken);

        ProjectTenantAccessProjection? saved = await store.GetAsync("tenant-a", cancellationToken);
        saved.ShouldNotBeNull();
        saved.Version.ShouldBe(1L);
        saved.Enabled.ShouldBeTrue();

        projection.Enabled = false;
        await Should.ThrowAsync<TenantAccessConcurrencyException>(() => store.SaveAsync(projection, cancellationToken));
    }

    /// <summary>Verifies project event journals rebuild list/detail projections through the shared fold.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldAppendDeduplicateAndRebuildListAndDetail()
    {
        FakeProjectsStateStore state = new();
        DaprProjectProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        EventEnvelope created = Envelope(
            "01J000000000000000000001",
            1,
            new ProjectCreated(
                "tenant-a",
                "project-a",
                "Project A",
                "metadata description",
                "setup-ref",
                ProjectLifecycle.Active,
                "user-a",
                "corr-a",
                "task-a",
                "idem-a",
                "fp-a",
                Now));
        EventEnvelope updated = Envelope(
            "01J000000000000000000002",
            2,
            new ProjectSetupUpdated(
                "tenant-a",
                "project-a",
                ProjectSetup.Empty,
                "user-a",
                "corr-b",
                "task-b",
                "idem-b",
                "fp-b",
                Now.AddMinutes(1)));

        (await store.AppendAsync(created, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);
        (await store.AppendAsync(created, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Duplicate);
        (await store.AppendAsync(updated, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);

        IReadOnlyList<ProjectListItem> rows = await store.ListAsync("tenant-a", null, cancellationToken);
        ProjectDetailItem? detail = await store.GetDetailAsync("tenant-a", "project-a", cancellationToken);

        rows.ShouldHaveSingleItem().ProjectId.ShouldBe("project-a");
        rows[0].Sequence.ShouldBe(2L);
        detail.ShouldNotBeNull();
        detail.Sequence.ShouldBe(2L);
        detail.Setup.ShouldNotBeNull();
    }

    /// <summary>Verifies tenant journals order events by EventStore global position, not aggregate sequence.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldAcceptMultipleProjectsWithPerAggregateSequenceNumbers()
    {
        FakeProjectsStateStore state = new();
        DaprProjectProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        EventEnvelope firstProject = Envelope(
            "01J000000000000000000010",
            1,
            new ProjectCreated("tenant-a", "project-a", "Project A", null, null, ProjectLifecycle.Active, "user-a", "corr-a", "task-a", "idem-a", "fp-a", Now),
            globalPosition: 10);
        EventEnvelope secondProject = Envelope(
            "01J000000000000000000011",
            1,
            new ProjectCreated("tenant-a", "project-b", "Project B", null, null, ProjectLifecycle.Active, "user-a", "corr-b", "task-b", "idem-b", "fp-b", Now.AddMinutes(1)),
            globalPosition: 11);

        (await store.AppendAsync(firstProject, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);
        (await store.AppendAsync(secondProject, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);

        IReadOnlyList<ProjectListItem> rows = await store.ListAsync("tenant-a", null, cancellationToken);

        rows.Select(static row => row.ProjectId).ShouldBe(["project-a", "project-b"]);
        rows.Max(static row => row.Sequence).ShouldBe(11L);
        (await store.GetReadinessAsync("tenant-a", cancellationToken)).Watermark.ShouldBe(11L);
    }

    /// <summary>Verifies replay conflicts are recorded and fail closed.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldDetectReplayConflictForSameMessageDifferentPayload()
    {
        FakeProjectsStateStore state = new();
        DaprProjectProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        EventEnvelope first = Envelope(
            "01J000000000000000000003",
            1,
            new ProjectCreated("tenant-a", "project-a", "Project A", null, null, ProjectLifecycle.Active, "user-a", "corr-a", "task-a", "idem-a", "fp-a", Now));
        EventEnvelope conflicting = Envelope(
            "01J000000000000000000003",
            1,
            new ProjectCreated("tenant-a", "project-a", "Project B", null, null, ProjectLifecycle.Active, "user-a", "corr-a", "task-a", "idem-a", "fp-different", Now));

        (await store.AppendAsync(first, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);
        (await store.AppendAsync(conflicting, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.ReplayConflict);
        (await store.GetReadinessAsync("tenant-a", cancellationToken)).ReplayConflict.ShouldBeTrue();
    }

    /// <summary>Verifies duplicate message IDs with incompatible metadata fail closed.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldDetectReplayConflictForSameMessageDifferentGlobalPosition()
    {
        FakeProjectsStateStore state = new();
        DaprProjectProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IProjectEvent projectEvent = new ProjectCreated(
            "tenant-a",
            "project-a",
            "Project A",
            null,
            null,
            ProjectLifecycle.Active,
            "user-a",
            "corr-a",
            "task-a",
            "idem-a",
            "fp-a",
            Now);

        EventEnvelope first = Envelope("01J000000000000000000020", 1, projectEvent, globalPosition: 20);
        EventEnvelope conflicting = Envelope("01J000000000000000000020", 1, projectEvent, globalPosition: 21);

        (await store.AppendAsync(first, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.Applied);
        (await store.AppendAsync(conflicting, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.ReplayConflict);

        (await store.GetReadinessAsync("tenant-a", cancellationToken)).ReplayConflict.ShouldBeTrue();
    }

    /// <summary>Verifies envelope/event identity mismatch marks the journal malformed and unreadable.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldFailClosedWhenEnvelopeTenantDiffersFromEventTenant()
    {
        FakeProjectsStateStore state = new();
        DaprProjectProjectionStore store = new(state);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        EventEnvelope mismatched = Envelope(
            "01J000000000000000000030",
            1,
            new ProjectCreated("tenant-b", "project-a", "Project A", null, null, ProjectLifecycle.Active, "user-a", "corr-a", "task-a", "idem-a", "fp-a", Now),
            globalPosition: 30,
            metadataTenantId: "tenant-a");

        (await store.AppendAsync(mismatched, cancellationToken)).Status.ShouldBe(ProjectProjectionAppendStatus.InvalidPayload);

        (await store.GetReadinessAsync("tenant-a", cancellationToken)).MalformedEvidence.ShouldBeTrue();
        await Should.ThrowAsync<InvalidOperationException>(() => store.ListAsync("tenant-a", null, cancellationToken));
    }

    /// <summary>Verifies runtime reads fail closed until a durable projection journal exists.</summary>
    [Fact]
    public async Task ProjectProjectionStoreShouldFailClosedWhenProjectionJournalIsMissing()
    {
        DaprProjectProjectionStore store = new(new FakeProjectsStateStore());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Should.ThrowAsync<InvalidOperationException>(() => store.ListAsync("tenant-a", null, cancellationToken));
        await Should.ThrowAsync<InvalidOperationException>(() => store.GetDetailAsync("tenant-a", "project-a", cancellationToken));
    }

    private static EventEnvelope Envelope(
        string messageId,
        long sequence,
        IProjectEvent projectEvent,
        long? globalPosition = null,
        string? metadataTenantId = null,
        string? metadataAggregateId = null)
        => new(
            new EventMetadata(
                messageId,
                metadataAggregateId ?? projectEvent.ProjectId,
                "project",
                metadataTenantId ?? projectEvent.TenantId,
                "projects",
                sequence,
                globalPosition ?? sequence,
                projectEvent.OccurredAt,
                projectEvent.CorrelationId,
                projectEvent.IdempotencyKey,
                "user-a",
                "1.0.0",
                projectEvent.GetType().FullName!,
                1,
                "json"),
            JsonSerializer.SerializeToUtf8Bytes(projectEvent, projectEvent.GetType(), DaprProjectProjectionStore.JsonOptions),
            null);

    private sealed class FakeProjectsStateStore : IProjectsStateStore
    {
        private readonly ConcurrentDictionary<string, StoredState> _states = new(StringComparer.Ordinal);

        public Task<ProjectsStateEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string stateKey = $"{storeName}:{key}";
            if (!_states.TryGetValue(stateKey, out StoredState? state))
            {
                return Task.FromResult(new ProjectsStateEntry<T>(default, null));
            }

            T? value = JsonSerializer.Deserialize<T>(state.Json, DaprProjectProjectionStore.JsonOptions);
            return Task.FromResult(new ProjectsStateEntry<T>(value, state.ETag));
        }

        public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string? eTag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string stateKey = $"{storeName}:{key}";
            string nextJson = JsonSerializer.Serialize(value, DaprProjectProjectionStore.JsonOptions);

            _states.AddOrUpdate(
                stateKey,
                _ => string.IsNullOrWhiteSpace(eTag) ? new StoredState(nextJson, "1") : throw new InvalidOperationException("Missing state cannot have an ETag."),
                (_, existing) =>
                {
                    if (!string.Equals(existing.ETag, eTag, StringComparison.Ordinal))
                    {
                        return existing;
                    }

                    return new StoredState(nextJson, (long.Parse(existing.ETag, System.Globalization.CultureInfo.InvariantCulture) + 1L).ToString(System.Globalization.CultureInfo.InvariantCulture));
                });

            StoredState current = _states[stateKey];
            return Task.FromResult(string.Equals(current.Json, nextJson, StringComparison.Ordinal) && !string.Equals(current.ETag, eTag, StringComparison.Ordinal));
        }

        private sealed record StoredState(string Json, string ETag);
    }
}
