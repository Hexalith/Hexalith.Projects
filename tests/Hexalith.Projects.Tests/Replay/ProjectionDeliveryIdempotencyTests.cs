// <copyright file="ProjectionDeliveryIdempotencyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Replay;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 FS-6 / NFR-7 duplicate &amp; out-of-order PROJECTION-event-delivery idempotency proofs (AC-3).
/// Proves the <b>projection-delivery</b> axis of at-least-once delivery: the same <c>ProjectCreated</c>
/// folded twice (same and tied sequence) is reflected exactly once, and out-of-order delivery converges
/// to the same final state as in-order delivery. Pure: no Dapr/Aspire/network/containers/browser; the
/// fold uses only event-carried data. Mirrors the Folders <c>DuplicateCreationEventsShouldReplay…</c>
/// pattern in xUnit v3 + Shouldly.
/// </summary>
/// <remarks>
/// <b>This file is the projection-delivery axis only.</b> It is kept physically separate from the
/// command-dedup proofs (<c>CommandDeliveryIdempotencyTests</c>, AC-2) because the epic / FS-6 require the
/// two properties to fail independently. The <see cref="ProjectStateApply"/> event-level replay-dedup
/// (by recorded fingerprint) is asserted here as its own case, distinct from the projection-fold
/// idempotency, to keep the three axes (command-delivery / projection-delivery / state-apply) visible.
/// </remarks>
public sealed class ProjectionDeliveryIdempotencyTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectId1 = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string ProjectId2 = "01HZ9K8YQ3W6V2N4R7T5P0X2CD";

    private static readonly DateTimeOffset Instant1 = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Instant2 = new(2026, 5, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ListProjection_DuplicateEnvelope_SameSequence_ReflectedExactlyOnce()
    {
        ProjectCreated created = Created(TenantA, ProjectId1, "Tracer Bullet", Instant1);

        ProjectListProjection projection = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, created),
            new ProjectProjectionEnvelope(TenantA, 1, created),
        ]);

        projection.Projects.Count.ShouldBe(1);
        projection.Get(TenantA, ProjectId1)!.Name.ShouldBe("Tracer Bullet");
        projection.Get(TenantA, ProjectId1)!.Lifecycle.ShouldBe(ProjectLifecycle.Active);
    }

    [Fact]
    public void DetailProjection_DuplicateEnvelope_TiedSequence_ReflectedExactlyOnce()
    {
        ProjectCreated created = Created(TenantA, ProjectId1, "Tracer Bullet", Instant1);

        // Tied sequence with an identical envelope: the (Sequence, IdempotencyKey, IdempotencyFingerprint)
        // tiebreaker collapses both to one row — no duplicate key, no double-count.
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 5, created),
            new ProjectProjectionEnvelope(TenantA, 5, created),
        ]);

        projection.Projects.Count.ShouldBe(1);
        projection.Get(TenantA, ProjectId1)!.Sequence.ShouldBe(5);
    }

    [Fact]
    public void ListProjection_OutOfOrderDelivery_ConvergesToInOrderState()
    {
        ProjectProjectionEnvelope[] inOrder =
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA, ProjectId1, "First", Instant1)),
            new ProjectProjectionEnvelope(TenantA, 2, Created(TenantA, ProjectId2, "Second", Instant2)),
            new ProjectProjectionEnvelope(TenantB, 3, Created(TenantB, ProjectId1, "Tenant B", Instant2)),
        ];
        ProjectProjectionEnvelope[] outOfOrder =
        [
            inOrder[2],
            inOrder[0],
            inOrder[1],
        ];

        ProjectListProjection ordered = ProjectListProjection.Rebuild(inOrder);
        ProjectListProjection shuffled = ProjectListProjection.Rebuild(outOfOrder);

        shuffled.Projects.Count.ShouldBe(ordered.Projects.Count);
        ordered.Projects.Count.ShouldBe(3); // exactly the number of distinct canonical keys.
        shuffled.Get(TenantA, ProjectId1)!.Name.ShouldBe(ordered.Get(TenantA, ProjectId1)!.Name);
        shuffled.Get(TenantA, ProjectId2)!.Name.ShouldBe(ordered.Get(TenantA, ProjectId2)!.Name);
        shuffled.Get(TenantB, ProjectId1)!.Name.ShouldBe(ordered.Get(TenantB, ProjectId1)!.Name);
    }

    [Fact]
    public void Projection_ProjectsCount_EqualsDistinctCanonicalKeyCount_UnderDuplicateAndReorder()
    {
        // Same logical events delivered with duplicates and out of order: the row count must equal the
        // number of DISTINCT canonical keys (two projects here), never the delivery count.
        ProjectCreated p1 = Created(TenantA, ProjectId1, "First", Instant1);
        ProjectCreated p2 = Created(TenantA, ProjectId2, "Second", Instant2);

        ProjectListProjection projection = ProjectListProjection.Rebuild(
        [
            new ProjectProjectionEnvelope(TenantA, 2, p2),
            new ProjectProjectionEnvelope(TenantA, 1, p1),
            new ProjectProjectionEnvelope(TenantA, 2, p2), // duplicate
            new ProjectProjectionEnvelope(TenantA, 1, p1), // duplicate
        ]);

        projection.Projects.Count.ShouldBe(2);
    }

    /// <summary>
    /// State-apply event-level replay dedup (AC-3), distinct from the projection-fold idempotency above:
    /// applying the same <c>ProjectCreated</c> (same recorded <c>IdempotencyKey</c> +
    /// <c>IdempotencyFingerprint</c>) twice via <see cref="ProjectStateApply"/> returns unchanged state —
    /// the at-least-once-delivery framing of the existing <c>IdenticalReplay_IsDeduped_StateUnchanged</c>.
    /// </summary>
    [Fact]
    public void StateApply_RedeliveredSameEvent_IsDeduped_StateUnchanged()
    {
        ProjectIdentity identity = new(TenantA, new ProjectId(ProjectId1));
        ProjectCreated created = Created(TenantA, ProjectId1, "Tracer Bullet", Instant1);

        ProjectState once = ProjectStateApply.Apply(ProjectState.Empty, created, identity);

        // Redelivery of the identical event (at-least-once) is deduped by the recorded fingerprint.
        ProjectState twice = ProjectStateApply.Apply(once, created, identity);

        twice.ShouldBeSameAs(once);
        twice.IdempotencyFingerprints.Count.ShouldBe(1);
    }

    [Fact]
    public void StateApply_FullStreamReplayWithDuplicate_IsDeduped()
    {
        ProjectIdentity identity = new(TenantA, new ProjectId(ProjectId1));
        ProjectCreated created = Created(TenantA, ProjectId1, "Tracer Bullet", Instant1);

        // Full-stream replay where the same event is delivered twice (at-least-once).
        IEnumerable<IProjectEvent> stream = [created, created];
        ProjectState state = ProjectState.Empty.Apply(stream, identity);

        state.IsCreated.ShouldBeTrue();
        state.IdempotencyFingerprints.Count.ShouldBe(1);
        state.Name.ShouldBe("Tracer Bullet");
    }

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
            "idem-" + tenant + "-" + projectId,
            "sha256:" + projectId.ToLowerInvariant(),
            occurredAt);
}
