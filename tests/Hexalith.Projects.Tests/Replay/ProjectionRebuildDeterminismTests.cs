// <copyright file="ProjectionRebuildDeterminismTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Replay;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Testing.Replay;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 FS-6 deterministic-rebuild proofs (AC-1) for the existing 1.4 read models and aggregate state.
/// Pure: no Dapr/Aspire/network/containers/browser; the fold uses only event-carried data, so every
/// assertion is deterministic with no wall-clock / random / GUID. Mirrors the Folders
/// <c>Folder*ProjectionReplayTests</c> design in xUnit v3 + Shouldly.
/// </summary>
/// <remarks>
/// <b>This file is the rebuild-determinism axis only</b> (rebuild == incremental, deterministic,
/// order-stable). Duplicate-command dedup (AC-2) lives in <c>CommandDeliveryIdempotencyTests</c> and
/// duplicate/out-of-order projection delivery (AC-3) lives in <c>ProjectionDeliveryIdempotencyTests</c>,
/// kept physically separate because the epic requires them to fail independently.
/// </remarks>
public sealed class ProjectionRebuildDeterminismTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectId1 = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string ProjectId2 = "01HZ9K8YQ3W6V2N4R7T5P0X2CD";
    private const string ProjectId3 = "01HZ9K8YQ3W6V2N4R7T5P0X3EF";

    // Fixed instants (never DateTimeOffset.Now) so the rebuilt watermark/timestamps are deterministic.
    private static readonly DateTimeOffset Instant1 = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Instant2 = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Instant3 = new(2026, 5, 25, 13, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Instant4 = new(2026, 5, 25, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Instant5 = new(2026, 5, 25, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ListProjection_FullConformanceMatrix_Holds()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectionRebuildConformance.AssertAll(
            stream,
            ProjectListProjection.Empty,
            ProjectListProjection.Rebuild,
            static (projection, envelope) => projection.Apply([envelope]),
            static projection => projection.Projects);
    }

    [Fact]
    public void DetailProjection_FullConformanceMatrix_Holds()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectionRebuildConformance.AssertAll(
            stream,
            ProjectDetailProjection.Empty,
            ProjectDetailProjection.Rebuild,
            static (projection, envelope) => projection.Apply([envelope]),
            static projection => projection.Projects);
    }

    [Fact]
    public void ListProjection_RebuildEqualsIncremental_SameEventsSameState()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectListProjection rebuilt = ProjectListProjection.Rebuild(stream);

        ProjectListProjection incremental = ProjectListProjection.Empty;
        foreach (ProjectProjectionEnvelope envelope in stream)
        {
            incremental = incremental.Apply([envelope]);
        }

        ProjectionRebuildConformance.AssertContentEqual(
            rebuilt.Projects, incremental.Projects, "list rebuild != incremental");
    }

    [Fact]
    public void DetailProjection_RebuildTwice_IsByteStableAndOrderStable()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectDetailProjection first = ProjectDetailProjection.Rebuild(stream);
        ProjectDetailProjection second = ProjectDetailProjection.Rebuild(stream);
        ProjectDetailProjection reversed = ProjectDetailProjection.Rebuild(stream.Reverse().ToArray());

        ProjectionRebuildConformance.AssertContentEqual(first.Projects, second.Projects, "detail rebuild not deterministic");
        ProjectionRebuildConformance.AssertContentEqual(first.Projects, reversed.Projects, "detail rebuild not order-stable");
    }

    [Fact]
    public void DetailProjection_RebuiltWatermarkAndTimestamps_ComeOnlyFromEventData_NoWallClock()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectDetailProjection rebuilt = ProjectDetailProjection.Rebuild(stream);

        // Project 1: create/setup/archive all fold from event-carried instants; latest sequence wins.
        ProjectDetailItem one = rebuilt.Get(TenantA, ProjectId1)!;
        one.Sequence.ShouldBe(5);
        one.CreatedAt.ShouldBe(Instant1);
        one.UpdatedAt.ShouldBe(Instant5);
        one.Setup.ShouldNotBeNull();
        one.Lifecycle.ShouldBe(ProjectLifecycle.Archived);

        // Project 2 (tenant A) and Project 3 (tenant B) carry their own fixed instants — never "now".
        rebuilt.Get(TenantA, ProjectId2)!.CreatedAt.ShouldBe(Instant2);
        rebuilt.Get(TenantA, ProjectId2)!.Sequence.ShouldBe(2);
        rebuilt.Get(TenantB, ProjectId3)!.CreatedAt.ShouldBe(Instant3);
        rebuilt.Get(TenantB, ProjectId3)!.Sequence.ShouldBe(3);
    }

    [Fact]
    public void ListProjection_RebuiltWatermark_ComesOnlyFromEventData()
    {
        IReadOnlyList<ProjectProjectionEnvelope> stream = Stream();

        ProjectListProjection rebuilt = ProjectListProjection.Rebuild(stream);

        rebuilt.Get(TenantA, ProjectId1)!.Sequence.ShouldBe(5);
        rebuilt.Get(TenantA, ProjectId1)!.CreatedAt.ShouldBe(Instant1);
        rebuilt.Get(TenantA, ProjectId1)!.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
        rebuilt.Projects.Count.ShouldBe(3);
    }

    /// <summary>
    /// FS-6 write-side mirror (AC-1): replaying the full event stream into <see cref="ProjectState"/>
    /// equals incrementally applying the same events one-by-one — same events -> same aggregate state.
    /// </summary>
    [Fact]
    public void AggregateState_FullStreamReplay_EqualsIncrementalApply()
    {
        ProjectIdentity identity = new(TenantA, new ProjectId(ProjectId1));
        IProjectEvent[] events =
        [
            Created(TenantA, ProjectId1, "Tracer Bullet", Instant1),
        ];

        // Full-stream replay.
        ProjectState fromReplay = ProjectState.Empty.Apply(events, identity);

        // One-by-one incremental apply.
        ProjectState incremental = ProjectState.Empty;
        foreach (IProjectEvent projectEvent in events)
        {
            incremental = ProjectStateApply.Apply(incremental, projectEvent, identity);
        }

        // ProjectState is an immutable record whose only reference member is the idempotency map; record
        // equality compares that by reference, so assert the load-bearing fields explicitly instead.
        fromReplay.IsCreated.ShouldBe(incremental.IsCreated);
        fromReplay.TenantId.ShouldBe(incremental.TenantId);
        fromReplay.ProjectId.ShouldBe(incremental.ProjectId);
        fromReplay.Name.ShouldBe(incremental.Name);
        fromReplay.Lifecycle.ShouldBe(incremental.Lifecycle);
        fromReplay.IdempotencyFingerprints.Count.ShouldBe(incremental.IdempotencyFingerprints.Count);
        fromReplay.IdempotencyFingerprints["idem-" + ProjectId1]
            .ShouldBe(incremental.IdempotencyFingerprints["idem-" + ProjectId1]);
    }

    // A small multi-project, multi-tenant stream (>= 2 tenants so isolation is implicitly exercised
    // under replay) that includes the Story 1.8 setup/archive events. Distinct sequences + distinct
    // idempotency keys/fingerprints keep rebuild unambiguous while the conformance helper exercises
    // reversed/rotated rebuilds and duplicate delivery.
    private static IReadOnlyList<ProjectProjectionEnvelope> Stream() =>
    [
        new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA, ProjectId1, "Tracer Bullet", Instant1)),
        new ProjectProjectionEnvelope(TenantA, 2, Created(TenantA, ProjectId2, "Second Project", Instant2)),
        new ProjectProjectionEnvelope(TenantB, 3, Created(TenantB, ProjectId3, "Tenant B Project", Instant3)),
        new ProjectProjectionEnvelope(TenantA, 4, SetupUpdated(TenantA, ProjectId1, Instant4)),
        new ProjectProjectionEnvelope(TenantA, 5, Archived(TenantA, ProjectId1, Instant5)),
    ];

    private static ProjectCreated Created(string tenant, string projectId, string name, DateTimeOffset occurredAt)
        => new(
            tenant,
            projectId,
            name,
            null,
            null,
            ProjectLifecycle.Active,
            "actor-001",
            "corr-" + projectId,
            "task-" + projectId,
            "idem-" + projectId,
            "sha256:" + projectId.ToLowerInvariant(),
            occurredAt);

    private static ProjectSetupUpdated SetupUpdated(string tenant, string projectId, DateTimeOffset occurredAt)
        => new(
            tenant,
            projectId,
            new ProjectSetup(
                ["keep continuity current"],
                ["use safe metadata"],
                [ProjectContextSourceKind.Conversation],
                [ProjectContextSourceKind.FileReference],
                new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences)),
            "actor-001",
            "corr-setup-" + projectId,
            "task-setup-" + projectId,
            "idem-setup-" + projectId,
            "sha256:setup-" + projectId.ToLowerInvariant(),
            occurredAt);

    private static ProjectArchived Archived(string tenant, string projectId, DateTimeOffset occurredAt)
        => new(
            tenant,
            projectId,
            ProjectLifecycle.Archived,
            "actor-001",
            "corr-archive-" + projectId,
            "task-archive-" + projectId,
            "idem-archive-" + projectId,
            "sha256:archive-" + projectId.ToLowerInvariant(),
            occurredAt);
}
