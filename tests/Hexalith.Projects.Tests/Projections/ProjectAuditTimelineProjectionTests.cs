// <copyright file="ProjectAuditTimelineProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Projections;

using System;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectList;

using Shouldly;

using Xunit;

/// <summary>Pure Tier-1 tests for the metadata-only Project audit timeline projection.</summary>
public sealed class ProjectAuditTimelineProjectionTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string OtherProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FileReferenceId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string MemoryReferenceId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";

    [Fact]
    public void Projection_MapsEverySuccessEventToMetadataOnlyAuditRows()
    {
        ProjectAuditTimelineProjection projection = ProjectAuditTimelineProjection.Rebuild(AllMappedEnvelopes());

        IReadOnlyList<ProjectAuditTimelineItem> rows = projection.List(TenantA, ProjectIdValue, limit: null);

        rows.Select(static row => row.OperationType).ShouldBe(
        [
            "project.resolution_confirmed",
            "memory.unlinked",
            "memory.linked",
            "file_reference.unlinked",
            "file_reference.linked",
            "project.folder_set",
            "project.folder_creation_pending",
            "project.restored",
            "project.archived",
            "project.setup_updated",
            "project.created",
        ]);
        rows.Select(static row => row.AuditEventId).Distinct(StringComparer.Ordinal).Count().ShouldBe(rows.Count);
        rows.ShouldAllBe(static row => row.TenantId == TenantA);
        rows.ShouldAllBe(static row => row.ProjectId == ProjectIdValue);
        rows.ShouldAllBe(static row => !string.IsNullOrWhiteSpace(row.ActorPrincipalId));
        rows.ShouldAllBe(static row => !string.IsNullOrWhiteSpace(row.CorrelationId));
        rows.ShouldAllBe(static row => !string.IsNullOrWhiteSpace(row.TaskId));
        rows.ShouldAllBe(static row => !string.IsNullOrWhiteSpace(row.IdempotencyKey));

        rows.Single(static row => row.OperationType == "project.archived").PreviousState.ShouldBe(ProjectLifecycle.Active.ToString());
        rows.Single(static row => row.OperationType == "project.archived").NewState.ShouldBe(ProjectLifecycle.Archived.ToString());
        rows.Single(static row => row.OperationType == "project.restored").PreviousState.ShouldBe(ProjectLifecycle.Archived.ToString());
        rows.Single(static row => row.OperationType == "project.restored").NewState.ShouldBe(ProjectLifecycle.Active.ToString());

        ProjectAuditTimelineItem pending = rows.Single(static row => row.OperationType == "project.folder_creation_pending");
        pending.ReferenceKind.ShouldBe("folder");
        pending.ReferenceId.ShouldBeNull();
        pending.NewState.ShouldBe(ReferenceState.Pending.ToString());
        pending.ReasonCode.ShouldBe("folder_create_external_unavailable");

        ProjectAuditTimelineItem folderSet = rows.Single(static row => row.OperationType == "project.folder_set");
        folderSet.ReferenceKind.ShouldBe("folder");
        folderSet.ReferenceId.ShouldBe(FolderId);
        folderSet.NewState.ShouldBe(ReferenceState.Included.ToString());

        ProjectAuditTimelineItem fileLink = rows.Single(static row => row.OperationType == "file_reference.linked");
        fileLink.ReferenceKind.ShouldBe("file");
        fileLink.ReferenceId.ShouldBe(FileReferenceId);
        fileLink.NewState.ShouldBe(ReferenceState.Included.ToString());

        ProjectAuditTimelineItem fileUnlink = rows.Single(static row => row.OperationType == "file_reference.unlinked");
        fileUnlink.ReferenceKind.ShouldBe("file");
        fileUnlink.ReferenceId.ShouldBe(FileReferenceId);
        fileUnlink.PreviousState.ShouldBe(ReferenceState.Included.ToString());
        fileUnlink.NewState.ShouldBe(ReferenceState.Excluded.ToString());

        ProjectAuditTimelineItem memoryLink = rows.Single(static row => row.OperationType == "memory.linked");
        memoryLink.ReferenceKind.ShouldBe("memory");
        memoryLink.ReferenceId.ShouldBe(MemoryReferenceId);

        ProjectAuditTimelineItem confirmed = rows.Single(static row => row.OperationType == "project.resolution_confirmed");
        confirmed.ReferenceKind.ShouldBe("conversation");
        confirmed.ReferenceId.ShouldBe("conversation-001");
        confirmed.SourceProjectId.ShouldBe("project-source-001");
    }

    [Fact]
    public void AuditEventId_IsStableAcrossRebuilds()
    {
        ProjectProjectionEnvelope envelope = new(TenantA, 1, Created());

        string first = ProjectAuditTimelineProjection.Rebuild([envelope]).List(TenantA, ProjectIdValue, null).Single().AuditEventId;
        string second = ProjectAuditTimelineProjection.Rebuild([envelope]).List(TenantA, ProjectIdValue, null).Single().AuditEventId;

        first.ShouldBe(second);
        first.ShouldStartWith("audit_");
    }

    [Fact]
    public void List_IsTenantScopedProjectFilterableNewestFirstAndLimitBounded()
    {
        ProjectAuditTimelineProjection projection = ProjectAuditTimelineProjection.Rebuild(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(projectId: ProjectIdValue, name: "A")),
            new ProjectProjectionEnvelope(TenantA, 2, SetupUpdated()),
            new ProjectProjectionEnvelope(TenantA, 3, Created(projectId: OtherProjectId, name: "Other")),
            new ProjectProjectionEnvelope(TenantB, 4, Created(tenant: TenantB, projectId: "01HZ9K8YQ3W6V2N4R7T5P0X1AD", name: "Foreign")),
        ]);

        projection.List(TenantA, projectId: null, limit: null)
            .Select(static row => row.ProjectId)
            .ShouldBe([OtherProjectId, ProjectIdValue, ProjectIdValue]);
        projection.List(TenantA, ProjectIdValue, limit: 1)
            .Select(static row => row.OperationType)
            .ShouldBe(["project.setup_updated"]);
        projection.List(TenantB, projectId: null, limit: null)
            .ShouldHaveSingleItem().ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AD");
    }

    [Fact]
    public void TenantGuard_SkipsEnvelopeEventTenantMismatch()
    {
        ProjectAuditTimelineProjection projection = ProjectAuditTimelineProjection.Rebuild(
        [
            new ProjectProjectionEnvelope(TenantB, 1, Created()),
        ]);

        projection.List(TenantA, projectId: null, limit: null).ShouldBeEmpty();
        projection.List(TenantB, projectId: null, limit: null).ShouldBeEmpty();
    }

    [Fact]
    public void DuplicateEnvelope_DoesNotDuplicateRows()
    {
        ProjectProjectionEnvelope envelope = new(TenantA, 1, Created());

        ProjectAuditTimelineProjection projection = ProjectAuditTimelineProjection.Rebuild([envelope, envelope]);

        projection.List(TenantA, ProjectIdValue, null).ShouldHaveSingleItem();
    }

    [Fact]
    public void UnknownEventType_Throws()
    {
        Should.Throw<InvalidOperationException>(() => ProjectAuditTimelineProjection.Rebuild(
        [
            new ProjectProjectionEnvelope(TenantA, 1, new UnknownProjectEvent()),
        ]));
    }

    [Fact]
    public void ProposalConfirmationChain_UsesConcreteEventsWithoutSyntheticProposalPayload()
    {
        ProjectAuditTimelineProjection projection = ProjectAuditTimelineProjection.Rebuild(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(idempotencyKey: "chain-create")),
            new ProjectProjectionEnvelope(TenantA, 2, FolderSet()),
            new ProjectProjectionEnvelope(TenantA, 3, FileLinked()),
        ]);

        string serialized = System.Text.Json.JsonSerializer.Serialize(projection.List(TenantA, ProjectIdValue, null));

        serialized.ShouldNotContain("ProjectCreatedFromProposal", Case.Insensitive);
        serialized.ShouldNotContain("candidate", Case.Insensitive);
        serialized.ShouldNotContain("score", Case.Insensitive);
        serialized.ShouldNotContain("rank", Case.Insensitive);
        projection.List(TenantA, ProjectIdValue, null)
            .Select(static row => row.OperationType)
            .ShouldBe(["file_reference.linked", "project.folder_set", "project.created"]);
    }

    private static ProjectProjectionEnvelope[] AllMappedEnvelopes() =>
    [
        new(TenantA, 1, Created()),
        new(TenantA, 2, SetupUpdated()),
        new(TenantA, 3, Archived()),
        new(TenantA, 4, Restored()),
        new(TenantA, 5, FolderPending()),
        new(TenantA, 6, FolderSet()),
        new(TenantA, 7, FileLinked()),
        new(TenantA, 8, FileUnlinked()),
        new(TenantA, 9, MemoryLinked()),
        new(TenantA, 10, MemoryUnlinked()),
        new(TenantA, 11, ResolutionConfirmed()),
    ];

    private static ProjectCreated Created(
        string tenant = TenantA,
        string projectId = ProjectIdValue,
        string name = "Tracer Bullet",
        string idempotencyKey = "idem-create")
        => new(
            tenant,
            projectId,
            name,
            "safe description",
            null,
            ProjectLifecycle.Active,
            "actor-001",
            "corr-create",
            "task-create",
            idempotencyKey,
            "sha256:" + idempotencyKey,
            DateTimeOffset.UnixEpoch);

    private static ProjectSetupUpdated SetupUpdated() => new(
        TenantA,
        ProjectIdValue,
        ProjectSetup.Empty,
        "actor-001",
        "corr-setup",
        "task-setup",
        "idem-setup",
        "sha256:setup",
        DateTimeOffset.UnixEpoch.AddMinutes(1));

    private static ProjectArchived Archived() => new(
        TenantA,
        ProjectIdValue,
        ProjectLifecycle.Archived,
        "actor-001",
        "corr-archive",
        "task-archive",
        "idem-archive",
        "sha256:archive",
        DateTimeOffset.UnixEpoch.AddMinutes(2));

    private static ProjectRestored Restored() => new(
        TenantA,
        ProjectIdValue,
        ProjectLifecycle.Active,
        "actor-001",
        "corr-restore",
        "task-restore",
        "idem-restore",
        "sha256:restore",
        DateTimeOffset.UnixEpoch.AddMinutes(2.5));

    private static ProjectFolderCreationPending FolderPending() => new(
        TenantA,
        ProjectIdValue,
        "Tracer Folder",
        "folder_create_external_unavailable",
        true,
        "actor-001",
        "corr-folder",
        "task-folder",
        "idem-folder-pending",
        "sha256:folder-pending",
        DateTimeOffset.UnixEpoch.AddMinutes(3));

    private static ProjectFolderSet FolderSet() => new(
        TenantA,
        ProjectIdValue,
        FolderId,
        new ProjectFolderMetadata("Tracer Folder"),
        "actor-001",
        "corr-folder",
        "task-folder",
        "idem-folder-set",
        "sha256:folder-set",
        DateTimeOffset.UnixEpoch.AddMinutes(4));

    private static FileReferenceLinked FileLinked() => new(
        TenantA,
        ProjectIdValue,
        FileReferenceId,
        FolderId,
        new ProjectFileReferenceMetadata("contract.pdf"),
        "actor-001",
        "corr-file",
        "task-file",
        "idem-file-link",
        "sha256:file-link",
        DateTimeOffset.UnixEpoch.AddMinutes(5));

    private static FileReferenceUnlinked FileUnlinked() => new(
        TenantA,
        ProjectIdValue,
        FileReferenceId,
        "actor-001",
        "corr-file",
        "task-file",
        "idem-file-unlink",
        "sha256:file-unlink",
        DateTimeOffset.UnixEpoch.AddMinutes(6));

    private static MemoryLinked MemoryLinked() => new(
        TenantA,
        ProjectIdValue,
        MemoryReferenceId,
        new ProjectMemoryReferenceMetadata("Q3 product strategy memory"),
        "actor-001",
        "corr-memory",
        "task-memory",
        "idem-memory-link",
        "sha256:memory-link",
        DateTimeOffset.UnixEpoch.AddMinutes(7));

    private static MemoryUnlinked MemoryUnlinked() => new(
        TenantA,
        ProjectIdValue,
        MemoryReferenceId,
        "actor-001",
        "corr-memory",
        "task-memory",
        "idem-memory-unlink",
        "sha256:memory-unlink",
        DateTimeOffset.UnixEpoch.AddMinutes(8));

    private static ProjectResolutionConfirmed ResolutionConfirmed() => new(
        TenantA,
        ProjectIdValue,
        "conversation-001",
        "project-source-001",
        "actor-001",
        "corr-resolution",
        "task-resolution",
        "idem-resolution",
        "sha256:resolution",
        DateTimeOffset.UnixEpoch.AddMinutes(9));

    private sealed record UnknownProjectEvent : IProjectEvent
    {
        public string TenantId => TenantA;

        public string ProjectId => ProjectIdValue;

        public string CorrelationId => "corr-unknown";

        public string TaskId => "task-unknown";

        public string IdempotencyKey => "idem-unknown";

        public string IdempotencyFingerprint => "sha256:unknown";

        public DateTimeOffset OccurredAt => DateTimeOffset.UnixEpoch;
    }
}
