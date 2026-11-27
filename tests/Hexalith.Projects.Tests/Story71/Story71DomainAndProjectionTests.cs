// <copyright file="Story71DomainAndProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Story71;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.SdkWriter.Storage;

using Shouldly;

using Xunit;

/// <summary>
/// Unconditional (runs in BOTH Debug source mode and <c>CI=true</c> Release package mode) unit tests
/// covering bound <see cref="ProjectCreated"/>, terminal <see cref="ProjectCreationAbandoned"/> fences
/// across <see cref="ProjectAggregate"/>, <see cref="ProjectStateApply"/>, all retained projections,
/// <see cref="ProjectReadModelFolder"/>, and <see cref="ReadModelBackedEventStore"/> monotonic global positions.
/// </summary>
public sealed class Story71DomainAndProjectionTests
{
    private const string TenantA = "tenant-a";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderIdValue = "fld-p-1234567890abcdef12345678";
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void BoundProjectCreated_ProjectsAcrossDetailListReferenceIndexAuditAndSupportedReadModel()
    {
        ProjectCreated bound = CreateBoundEvent();
        ProjectProjectionEnvelope envelope = new(TenantA, 1, bound);

        // 1. ProjectDetailProjection
        ProjectDetailItem detail = ProjectDetailProjection.Empty.Apply([envelope]).Get(TenantA, ProjectIdValue).ShouldNotBeNull();
        detail.Name.ShouldBe("Bound Alpha");
        detail.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        detail.ProjectFolder.ShouldNotBeNull();
        detail.ProjectFolder!.FolderId.ShouldBe(FolderIdValue);
        detail.ProjectFolder.DisplayName.ShouldBe("Alpha Folder");
        detail.ProjectFolder.ReferenceState.ShouldBe(ReferenceState.Included);

        // 2. ProjectListProjection
        ProjectListItem listItem = ProjectListProjection.Empty.Apply([envelope]).Get(TenantA, ProjectIdValue).ShouldNotBeNull();
        listItem.Name.ShouldBe("Bound Alpha");
        listItem.Lifecycle.ShouldBe(ProjectLifecycle.Active);

        // 3. ProjectReferenceIndexProjection
        IReadOnlyList<ProjectReferenceIndexItem> refs = ProjectReferenceIndexProjection.Empty.Apply([envelope]).List(TenantA, ProjectIdValue);
        ProjectReferenceIndexItem folderRef = refs.ShouldHaveSingleItem();
        folderRef.ReferenceKind.ShouldBe("folder");
        folderRef.ReferenceId.ShouldBe(FolderIdValue);
        folderRef.DisplayName.ShouldBe("Alpha Folder");
        folderRef.ReferenceState.ShouldBe(ReferenceState.Included);

        // 4. ProjectAuditTimelineProjection (ReferenceKind + ReferenceId + Safe Metadata ToSupportedFact)
        ProjectAuditTimelineItem auditRow = ProjectAuditTimelineProjection.Empty.Apply([envelope]).List(TenantA, ProjectIdValue, null).ShouldHaveSingleItem();
        auditRow.OperationType.ShouldBe("project.created");
        auditRow.ReferenceKind.ShouldBe("folder");
        auditRow.ReferenceId.ShouldBe(FolderIdValue);
        ProjectAuditFact fact = ProjectAuditTimelineProjection.ToSupportedFact(auditRow).ShouldNotBeNull();
        fact.Operation.ShouldBe("project.created");
        fact.Outcome.ShouldBe("Active");

        // 5. ProjectReadModelFolder.Fold (AD-24 supported read fold)
        ProjectReadModel model = ProjectReadModelFolder.Fold(
            new ProjectionRequest(TenantA, "projects", ProjectIdValue, [ToDto(bound, 1, 100)]),
            previous: null);
        model.Freshness.ShouldBe(EvidenceFreshnessState.Current);
        model.IsCreationAbandoned.ShouldBeFalse();
        model.Detail.ShouldNotBeNull();
        model.Detail!.ProjectFolder.ShouldNotBeNull();
        model.Detail.ProjectFolder!.FolderId.ShouldBe(FolderIdValue);
        model.Detail.ProjectFolder.DisplayName.ShouldBe("Alpha Folder");
    }

    [Fact]
    public void ProjectAggregate_HandleCreateProject_RejectsAbandonedStream()
    {
        ProjectIdentity identity = new(TenantA, new ProjectId(ProjectIdValue));
        ProjectState abandonedState = ProjectStateApply.Apply(ProjectState.Empty, CreateAbandonedEvent(), identity);
        abandonedState.IsCreationAbandoned.ShouldBeTrue();
        abandonedState.IsCreated.ShouldBeFalse();

        CreateProject lateCreate = new(
            TenantA,
            new ProjectId(ProjectIdValue),
            "Late Project",
            null,
            null,
            "actor-1",
            "corr-late",
            "task-late",
            "idem-late");

        ProjectResult result = ProjectAggregate.Handle(abandonedState, lateCreate, BaseTime.AddSeconds(5));
        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.CreationAbandoned);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void ProjectStateApply_EnforcesMutualExclusionAndBlocksAllMutatingEventsAfterAbandonment()
    {
        ProjectIdentity identity = new(TenantA, new ProjectId(ProjectIdValue));
        ProjectState createdState = ProjectStateApply.Apply(ProjectState.Empty, CreateBoundEvent(), identity);

        // Duplicate ProjectCreated (different idempotency key) must throw.
        Should.Throw<InvalidOperationException>(() => ProjectStateApply.Apply(
            createdState,
            CreateBoundEvent(idempotencyKey: "idem-dup", fingerprint: "sha256:dup"),
            identity));

        // ProjectCreationAbandoned after ProjectCreated must throw.
        Should.Throw<InvalidOperationException>(() => ProjectStateApply.Apply(
            createdState,
            CreateAbandonedEvent(),
            identity));

        // After abandonment, EVERY mutating event branch must throw!
        ProjectState abandonedState = ProjectStateApply.Apply(ProjectState.Empty, CreateAbandonedEvent(), identity);
        IProjectEvent[] forbiddenAfterAbandonment =
        [
            CreateBoundEvent(idempotencyKey: "idem-c2", fingerprint: "sha256:c2"),
            CreateAbandonedEvent(idempotencyKey: "idem-a2", fingerprint: "sha256:a2"),
            new ProjectSetupUpdated(TenantA, ProjectIdValue, ProjectSetup.Empty, "actor-1", "c", "t", "k1", "f1", BaseTime.AddSeconds(2)),
            new ProjectFolderSet(TenantA, ProjectIdValue, FolderIdValue, new("Folder"), "actor-1", "c", "t", "k2", "f2", BaseTime.AddSeconds(2)),
            new ProjectFolderCreationPending(TenantA, ProjectIdValue, "Name", "pending", true, "actor-1", "c", "t", "k3", "f3", BaseTime.AddSeconds(2)),
            new FileReferenceLinked(TenantA, ProjectIdValue, "file-1", FolderIdValue, new("File"), "actor-1", "c", "t", "k4", "f4", BaseTime.AddSeconds(2)),
            new FileReferenceUnlinked(TenantA, ProjectIdValue, "file-1", "actor-1", "c", "t", "k5", "f5", BaseTime.AddSeconds(2)),
            new MemoryLinked(TenantA, ProjectIdValue, "mem-1", new("Mem"), "actor-1", "c", "t", "k6", "f6", BaseTime.AddSeconds(2)),
            new MemoryUnlinked(TenantA, ProjectIdValue, "mem-1", "actor-1", "c", "t", "k7", "f7", BaseTime.AddSeconds(2)),
            new ProjectArchived(TenantA, ProjectIdValue, ProjectLifecycle.Archived, "actor-1", "c", "t", "k8", "f8", BaseTime.AddSeconds(2)),
            new ProjectRestored(TenantA, ProjectIdValue, ProjectLifecycle.Active, "actor-1", "c", "t", "k9", "f9", BaseTime.AddSeconds(2)),
            new ProjectResolutionConfirmed(TenantA, ProjectIdValue, "conv-1", null, "actor-1", "c", "t", "k10", "f10", BaseTime.AddSeconds(2)),
            new ProjectChatBotAuthorityGranted(TenantA, ProjectIdValue, "p1", "op", "human", "Ui", "g1", BaseTime.AddHours(1), "actor-1", "c", "t", "k11", "f11", BaseTime.AddSeconds(2)),
            new ProjectChatBotAuthorityRevoked(TenantA, ProjectIdValue, "p1", "op", "human", "Ui", "g1", "actor-1", "c", "t", "k12", "f12", BaseTime.AddSeconds(2)),
        ];

        foreach (IProjectEvent forbidden in forbiddenAfterAbandonment)
        {
            Should.Throw<InvalidOperationException>(() => ProjectStateApply.Apply(abandonedState, forbidden, identity), forbidden.GetType().Name);
        }

        // Exact idempotent replay of the SAME abandonment event is a harmless no-op.
        ProjectStateApply.Apply(abandonedState, CreateAbandonedEvent(), identity).ShouldBe(abandonedState);
    }

    [Fact]
    public void RetainedProjections_EnforceAbandonmentFenceThrowAfterCreationAndSkipMalformedKeys()
    {
        ProjectProjectionEnvelope createdEnv = new(TenantA, 1, CreateBoundEvent());
        ProjectProjectionEnvelope abandonedAfterEnv = new(TenantA, 2, CreateAbandonedEvent());

        // Post-creation abandonment must THROW in Detail, List, and ReferenceIndex projections.
        Should.Throw<InvalidOperationException>(() => ProjectDetailProjection.Empty.Apply([createdEnv, abandonedAfterEnv]));
        Should.Throw<InvalidOperationException>(() => ProjectListProjection.Empty.Apply([createdEnv, abandonedAfterEnv]));
        Should.Throw<InvalidOperationException>(() => ProjectReferenceIndexProjection.Empty.Apply([createdEnv, abandonedAfterEnv]));

        // Pre-creation abandonment fences late ProjectCreated and all reference events.
        ProjectProjectionEnvelope abandonFirst = new(TenantA, 1, CreateAbandonedEvent());
        ProjectProjectionEnvelope lateCreate = new(TenantA, 2, CreateBoundEvent());
        ProjectProjectionEnvelope lateFolderSet = new(TenantA, 3, new ProjectFolderSet(TenantA, ProjectIdValue, FolderIdValue, new("Late"), "actor-1", "c", "t", "k1", "f1", BaseTime.AddSeconds(3)));
        ProjectProjectionEnvelope lateFile = new(TenantA, 4, new FileReferenceLinked(TenantA, ProjectIdValue, "file-1", FolderIdValue, new("File"), "actor-1", "c", "t", "k2", "f2", BaseTime.AddSeconds(4)));
        ProjectProjectionEnvelope lateMemory = new(TenantA, 5, new MemoryLinked(TenantA, ProjectIdValue, "mem-1", new("Mem"), "actor-1", "c", "t", "k3", "f3", BaseTime.AddSeconds(5)));

        ProjectDetailProjection.Empty.Apply([abandonFirst, lateCreate, lateFolderSet, lateFile, lateMemory]).Get(TenantA, ProjectIdValue).ShouldBeNull();
        ProjectListProjection.Empty.Apply([abandonFirst, lateCreate]).Get(TenantA, ProjectIdValue).ShouldBeNull();
        ProjectReferenceIndexProjection.Empty.Apply([abandonFirst, lateCreate, lateFolderSet, lateFile, lateMemory]).References.ShouldBeEmpty();

        // Audit timeline records project.creation_abandoned and maps Safe Metadata ToSupportedFact.
        ProjectAuditTimelineItem auditItem = ProjectAuditTimelineProjection.Empty.Apply([abandonFirst]).List(TenantA, ProjectIdValue, null).ShouldHaveSingleItem();
        auditItem.OperationType.ShouldBe("project.creation_abandoned");
        auditItem.NewState.ShouldBe("Abandoned");
        auditItem.ReasonCode.ShouldBe("claim_released");
        ProjectAuditFact fact = ProjectAuditTimelineProjection.ToSupportedFact(auditItem).ShouldNotBeNull();
        fact.Operation.ShouldBe("project.creation_abandoned");
        fact.Outcome.ShouldBe("Abandoned");

        // Malformed TenantId/ProjectId in ProjectReferenceIndexProjection must skip (never write bare FolderId key).
        ProjectFolderSet malformed = new(TenantA, "   ", FolderIdValue, new("Bad"), "actor-1", "c", "t", "k-bad", "f-bad", BaseTime);
        ProjectReferenceIndexProjection.Empty.Apply([new ProjectProjectionEnvelope(TenantA, 1, malformed)]).References.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("bad-receipt")]
    [InlineData("gen-without-folder")]
    [InlineData("display-without-folder")]
    [InlineData("nonpositive-gen")]
    [InlineData("unsafe-taskstepkey")]
    [InlineData("abandoned-gen-without-folder")]
    [InlineData("abandoned-unsafe-taskstepkey")]
    public void ProjectReadModelFolder_RejectsMalformedBoundAndAbandonedPayloads(string caseKind)
    {
        IProjectEvent badEvent = caseKind switch
        {
            "bad-receipt" => CreateBoundEvent() with { FolderProvisioningReceiptId = "prov-bad\r\n" },
            "gen-without-folder" => CreateBoundEvent() with { FolderId = null, FolderDisplayName = null, FolderProvisioningReceiptId = null, FolderClaimGeneration = 1 },
            "display-without-folder" => CreateBoundEvent() with { FolderId = null, FolderDisplayName = "Orphan Name", FolderProvisioningReceiptId = null, FolderClaimGeneration = null },
            "nonpositive-gen" => CreateBoundEvent() with { FolderClaimGeneration = 0 },
            "unsafe-taskstepkey" => CreateBoundEvent() with { TaskStepKey = "task-1/step/3\u2028" },
            "abandoned-gen-without-folder" => CreateAbandonedEvent() with { FolderId = null, FolderClaimGeneration = 1 },
            "abandoned-unsafe-taskstepkey" => CreateAbandonedEvent() with { TaskStepKey = "task-1/abandon\n" },
            _ => throw new ArgumentOutOfRangeException(nameof(caseKind)),
        };

        ProjectReadModel result = ProjectReadModelFolder.Fold(
            new ProjectionRequest(TenantA, "projects", ProjectIdValue, [ToDto(badEvent, 1, 100)]),
            previous: null);
        result.Freshness.ShouldBe(EvidenceFreshnessState.Unavailable);
    }

    [Fact]
    public async Task ReadModelBackedEventStore_AllocatesStrictlyMonotonicGlobalPositionsAcrossReplicasAndRestarts()
    {
        Story71InMemoryReadModelStore sharedStore = new();
        Story71ManualTimeProvider clock = new(BaseTime);

        // Two independent replicas + a restarted instance sharing the same IReadModelStore.
        ReadModelBackedEventStore replicaA = new(sharedStore, ProjectReadModelKeys.StoreName, clock);
        ReadModelBackedEventStore replicaB = new(sharedStore, ProjectReadModelKeys.StoreName, clock);

        System.Threading.CancellationToken ct = TestContext.Current.CancellationToken;
        StoredEventStreamAppendResult r1 = await replicaA.TryAppendAsync(
            TenantA, "projects", "proj-1", 0, [CreateBoundEvent(projectId: "proj-1")], "actor-1", "c1", "m1", ct);
        StoredEventStreamAppendResult r2 = await replicaB.TryAppendAsync(
            TenantA, "projects", "proj-2", 0, [CreateBoundEvent(projectId: "proj-2")], "actor-1", "c2", "m2", ct);

        // Simulated restart (fresh instance against the same store).
        ReadModelBackedEventStore restarted = new(sharedStore, ProjectReadModelKeys.StoreName, clock);
        StoredEventStreamAppendResult r3 = await restarted.TryAppendAsync(
            TenantA, "projects", "proj-3", 0, [CreateBoundEvent(projectId: "proj-3")], "actor-1", "c3", "m3", ct);

        r1.Succeeded.ShouldBeTrue();
        r2.Succeeded.ShouldBeTrue();
        r3.Succeeded.ShouldBeTrue();

        long[] positions = [r1.Stream.Events[0].GlobalPosition, r2.Stream.Events[0].GlobalPosition, r3.Stream.Events[0].GlobalPosition];
        positions.ShouldBe([1L, 2L, 3L]);

        // Stale expectedVersion fails closed with ConcurrencyConflict.
        StoredEventStreamAppendResult conflict = await replicaA.TryAppendAsync(
            TenantA, "projects", "proj-1", 0, [CreateBoundEvent(projectId: "proj-1")], "actor-1", "c4", "m4", ct);
        conflict.Succeeded.ShouldBeFalse();
        conflict.ConcurrencyConflict.ShouldBeTrue();
        conflict.Stream.Version.ShouldBe(1);
    }

    private static ProjectCreated CreateBoundEvent(
        string projectId = ProjectIdValue,
        string idempotencyKey = "idem-1",
        string fingerprint = "sha256:bound1")
        => new(
            TenantA,
            projectId,
            "Bound Alpha",
            "Safe description",
            "setup-ref",
            ProjectLifecycle.Active,
            "actor-1",
            "corr-1",
            "task-1",
            idempotencyKey,
            fingerprint,
            BaseTime)
        {
            FolderId = FolderIdValue,
            FolderDisplayName = "Alpha Folder",
            FolderProvisioningReceiptId = "prov-1234567890abcdef",
            FolderClaimGeneration = 1,
            TaskStepKey = "task-1/step/3",
        };

    private static ProjectCreationAbandoned CreateAbandonedEvent(
        string idempotencyKey = "idem-abandon",
        string fingerprint = "sha256:abandon")
        => new(
            TenantA,
            ProjectIdValue,
            FolderIdValue,
            1,
            "claim_released",
            "actor-1",
            "corr-abandon",
            "task-abandon",
            idempotencyKey,
            fingerprint,
            BaseTime.AddSeconds(1))
        {
            TaskStepKey = "task-abandon/abandon",
        };

    private static ProjectionEventDto ToDto(IProjectEvent ev, long sequence, long globalPosition)
        => new(
            ev.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(ev, ev.GetType(), JsonOptions),
            "json",
            sequence,
            ev.OccurredAt,
            "projects",
            ev.CorrelationId,
            "actor-1",
            globalPosition);
}
