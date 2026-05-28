// <copyright file="ProjectProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Projections;

using System;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for <see cref="ProjectListProjection"/> + <see cref="ProjectDetailProjection"/>
/// (AC 1, 6, 7): apply <c>ProjectCreated</c>, deterministic ordering, tenant-guard a foreign event,
/// throw on unknown event, and the FS-8/SM-3 cross-tenant isolation negative test.
/// </summary>
public sealed class ProjectProjectionTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void ListProjection_AppliesProjectCreated()
    {
        ProjectListProjection projection = ProjectListProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA))]);

        projection.Contains(TenantA, ProjectIdValue).ShouldBeTrue();
        ProjectListItem item = projection.Get(TenantA, ProjectIdValue)!;
        item.Name.ShouldBe("Tracer Bullet");
        item.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        item.Sequence.ShouldBe(1);
        item.UpdatedAt.ShouldBe(item.CreatedAt);
    }

    [Fact]
    public void DetailProjection_AppliesProjectCreated()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA))]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;
        detail.Name.ShouldBe("Tracer Bullet");
        detail.Lifecycle.ShouldBe(ProjectLifecycle.Active);
    }

    [Fact]
    public void DetailProjection_AppliesProjectSetupUpdated()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, SetupUpdated(TenantA)),
        ]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;
        detail.Setup.ShouldNotBeNull();
        detail.Setup.Goals.ShouldBe(["keep continuity current"]);
        detail.UpdatedAt.ShouldBe(DateTimeOffset.UnixEpoch.AddMinutes(1));
        detail.Sequence.ShouldBe(2);
    }

    [Fact]
    public void ListProjection_SetupUpdatedKeepsRowMetadataOnly()
    {
        ProjectListProjection projection = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, SetupUpdated(TenantA)),
        ]);

        ProjectListItem item = projection.Get(TenantA, ProjectIdValue)!;
        item.Name.ShouldBe("Tracer Bullet");
        item.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        item.UpdatedAt.ShouldBe(DateTimeOffset.UnixEpoch.AddMinutes(1));
        item.Sequence.ShouldBe(2);
    }

    [Fact]
    public void Projections_ArchiveUpdatesLifecycleAndListFilters()
    {
        ProjectListProjection list = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, Archived(TenantA)),
        ]);
        ProjectDetailProjection detail = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, Archived(TenantA)),
        ]);

        list.Get(TenantA, ProjectIdValue)!.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
        list.List(TenantA, ProjectLifecycle.Active).ShouldBeEmpty();
        list.List(TenantA, ProjectLifecycle.Archived).Single().ProjectId.ShouldBe(ProjectIdValue);
        detail.Get(TenantA, ProjectIdValue)!.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
    }

    [Fact]
    public void DetailProjection_AppliesProjectFolderCreationPending()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, FolderPending(TenantA)),
        ]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;
        detail.ProjectFolder.ShouldNotBeNull();
        detail.ProjectFolder.ReferenceState.ShouldBe(ReferenceState.Pending);
        detail.ProjectFolder.ReasonCode.ShouldBe("folder_create_external_unavailable");
    }

    [Fact]
    public void DetailProjection_AppliesProjectFolderSet()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, FolderPending(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 3, FolderSet(TenantA)),
        ]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;
        detail.ProjectFolder.ShouldNotBeNull();
        detail.ProjectFolder.ReferenceState.ShouldBe(ReferenceState.Included);
        detail.ProjectFolder.FolderId.ShouldBe("folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC");
        detail.Sequence.ShouldBe(3);
    }

    [Fact]
    public void DetailProjection_AppliesFileReferenceLinkedAndUnlinked()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, FolderSet(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 3, FileLinked(TenantA, "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(TenantA, 4, FileLinked(TenantA, "file_01HZ9K8YQ3W6V2N4R7T5P0X1F2")),
            new ProjectProjectionEnvelope(TenantA, 5, FileUnlinked(TenantA, "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
        ]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;

        // The Project Folder survives file link/unlink.
        detail.ProjectFolder!.ReferenceState.ShouldBe(ReferenceState.Included);
        detail.FileReferences.Select(reference => reference.FileReferenceId)
            .ShouldBe(["file_01HZ9K8YQ3W6V2N4R7T5P0X1F2"]);
        detail.FileReferences[0].ReferenceState.ShouldBe(ReferenceState.Included);
        detail.FileReferences[0].DisplayName.ShouldBe("contract.pdf");
        detail.Sequence.ShouldBe(5);
    }

    [Fact]
    public void DetailProjection_AppliesMemoryLinkedAndUnlinked()
    {
        ProjectDetailProjection projection = ProjectDetailProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 2, FolderSet(TenantA)),
            new ProjectProjectionEnvelope(TenantA, 3, FileLinked(TenantA, "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(TenantA, 4, MemoryLink(TenantA, "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
            new ProjectProjectionEnvelope(TenantA, 5, MemoryLink(TenantA, "case_01HZ9K8YQ3W6V2N4R7T5P0X1M2")),
            new ProjectProjectionEnvelope(TenantA, 6, MemoryUnlink(TenantA, "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
        ]);

        ProjectDetailItem detail = projection.Get(TenantA, ProjectIdValue)!;

        // Memory link/unlink never touches the Project Folder or file references.
        detail.ProjectFolder!.ReferenceState.ShouldBe(ReferenceState.Included);
        detail.FileReferences.Count.ShouldBe(1);
        detail.MemoryReferences.Select(reference => reference.MemoryReferenceId)
            .ShouldBe(["case_01HZ9K8YQ3W6V2N4R7T5P0X1M2"]);
        detail.MemoryReferences[0].ReferenceState.ShouldBe(ReferenceState.Included);
        detail.MemoryReferences[0].DisplayName.ShouldBe("Q3 product strategy memory");
        detail.Sequence.ShouldBe(6);
    }

    [Fact]
    public void ListProjection_ForeignTenantEnvelope_IsSkipped()
    {
        // Envelope dispatch tenant B but event tenant A → tenant-guard skips it (never lands in B).
        ProjectListProjection projection = ProjectListProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantB, 1, Created(TenantA))]);

        projection.Projects.ShouldBeEmpty();
    }

    [Fact]
    public void ListProjection_DeterministicOrdering_AcrossEqualSequences()
    {
        ProjectCreated first = Created(TenantA, idempotencyKey: "key-aaa", fingerprint: "sha256:aaa");
        ProjectCreated second = Created(TenantA, idempotencyKey: "key-bbb", fingerprint: "sha256:bbb", name: "Reordered");

        // Same sequence; the tiebreaker (idempotency key ordinal) makes the fold deterministic.
        ProjectListProjection forward = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, first),
            new ProjectProjectionEnvelope(TenantA, 1, second),
        ]);
        ProjectListProjection reverse = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, second),
            new ProjectProjectionEnvelope(TenantA, 1, first),
        ]);

        forward.Get(TenantA, ProjectIdValue)!.Name.ShouldBe(reverse.Get(TenantA, ProjectIdValue)!.Name);
    }

    [Fact]
    public void ListProjection_ListFiltersByTenantAndLifecycle()
    {
        const string archivedProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AC";
        ProjectListProjection projection = ProjectListProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA, name: "Active")),
            new ProjectProjectionEnvelope(TenantA, 2, Created(TenantA, projectId: archivedProjectId, lifecycle: ProjectLifecycle.Archived, name: "Archived")),
            new ProjectProjectionEnvelope(TenantB, 3, Created(TenantB, projectId: "01HZ9K8YQ3W6V2N4R7T5P0X1AD", name: "Foreign")),
        ]);

        projection.List(TenantA, ProjectLifecycle.Active).Select(item => item.ProjectId)
            .ShouldBe([ProjectIdValue]);
        projection.List(TenantA, ProjectLifecycle.Archived).Select(item => item.ProjectId)
            .ShouldBe([archivedProjectId]);
        projection.List(TenantA, lifecycleFilter: null).Select(item => item.ProjectId)
            .ShouldBe([ProjectIdValue, archivedProjectId]);
        projection.List(TenantB, lifecycleFilter: null).Select(item => item.ProjectId)
            .ShouldBe(["01HZ9K8YQ3W6V2N4R7T5P0X1AD"]);
    }

    [Fact]
    public void ListProjection_UnknownEventType_Throws()
    {
        Should.Throw<InvalidOperationException>(() => ProjectListProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, new UnknownProjectEvent())]));
    }

    [Fact]
    public void DetailProjection_UnknownEventType_Throws()
    {
        Should.Throw<InvalidOperationException>(() => ProjectDetailProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, new UnknownProjectEvent())]));
    }

    /// <summary>
    /// FS-8 / SM-3 cross-tenant isolation negative test: a project created in tenant A never appears in
    /// a tenant-B query on this trivial event set.
    /// </summary>
    [Fact]
    public void CrossTenantIsolation_ProjectInTenantA_NeverAppearsInTenantBQuery()
    {
        ProjectListProjection list = ProjectListProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA))]);
        ProjectDetailProjection detail = ProjectDetailProjection.Empty
            .Apply([new ProjectProjectionEnvelope(TenantA, 1, Created(TenantA))]);

        // Present for tenant A.
        list.Contains(TenantA, ProjectIdValue).ShouldBeTrue();
        detail.Get(TenantA, ProjectIdValue).ShouldNotBeNull();

        // Absent for tenant B (same project id, different tenant → disjoint canonical key).
        list.Contains(TenantB, ProjectIdValue).ShouldBeFalse();
        list.Get(TenantB, ProjectIdValue).ShouldBeNull();
        detail.Get(TenantB, ProjectIdValue).ShouldBeNull();
    }

    private static ProjectCreated Created(
        string tenant,
        string idempotencyKey = "idem-key-001",
        string fingerprint = "sha256:deadbeef",
        string name = "Tracer Bullet",
        string projectId = ProjectIdValue,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active)
        => new(
            tenant,
            projectId,
            name,
            null,
            null,
            lifecycle,
            "actor-001",
            "corr-001",
            "task-001",
            idempotencyKey,
            fingerprint,
            DateTimeOffset.UnixEpoch);

    private static ProjectSetupUpdated SetupUpdated(string tenant)
        => new(
            tenant,
            ProjectIdValue,
            new ProjectSetup(
                ["keep continuity current"],
                ["use safe metadata"],
                [ProjectContextSourceKind.Conversation],
                [ProjectContextSourceKind.FileReference],
                new ConversationStartDefaults(LinkedSourcePolicy.ProjectsOwnedMetadataOnly)),
            "actor-001",
            "corr-setup",
            "task-setup",
            "idem-key-setup",
            "sha256:setup",
            DateTimeOffset.UnixEpoch.AddMinutes(1));

    private static ProjectArchived Archived(string tenant)
        => new(
            tenant,
            ProjectIdValue,
            ProjectLifecycle.Archived,
            "actor-001",
            "corr-archive",
            "task-archive",
            "idem-key-archive",
            "sha256:archive",
            DateTimeOffset.UnixEpoch.AddMinutes(2));

    private static ProjectFolderCreationPending FolderPending(string tenant)
        => new(
            tenant,
            ProjectIdValue,
            "Tracer Bullet",
            "folder_create_external_unavailable",
            true,
            "actor-001",
            "corr-folder",
            "task-folder",
            "idem-key-folder-pending",
            "sha256:folder-pending",
            DateTimeOffset.UnixEpoch.AddMinutes(3));

    private static ProjectFolderSet FolderSet(string tenant)
        => new(
            tenant,
            ProjectIdValue,
            "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            new ProjectFolderMetadata("Tracer Folder"),
            "actor-001",
            "corr-folder",
            "task-folder",
            "idem-key-folder-set",
            "sha256:folder-set",
            DateTimeOffset.UnixEpoch.AddMinutes(4));

    private static FileReferenceLinked FileLinked(string tenant, string fileReferenceId)
        => new(
            tenant,
            ProjectIdValue,
            fileReferenceId,
            "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            new ProjectFileReferenceMetadata("contract.pdf"),
            "actor-001",
            "corr-file",
            "task-file",
            "idem-key-" + fileReferenceId,
            "sha256:" + fileReferenceId,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

    private static FileReferenceUnlinked FileUnlinked(string tenant, string fileReferenceId)
        => new(
            tenant,
            ProjectIdValue,
            fileReferenceId,
            "actor-001",
            "corr-file",
            "task-file",
            "idem-key-unlink-" + fileReferenceId,
            "sha256:unlink-" + fileReferenceId,
            DateTimeOffset.UnixEpoch.AddMinutes(6));

    private static MemoryLinked MemoryLink(string tenant, string memoryReferenceId)
        => new(
            tenant,
            ProjectIdValue,
            memoryReferenceId,
            new ProjectMemoryReferenceMetadata("Q3 product strategy memory"),
            "actor-001",
            "corr-memory",
            "task-memory",
            "idem-key-" + memoryReferenceId,
            "sha256:" + memoryReferenceId,
            DateTimeOffset.UnixEpoch.AddMinutes(7));

    private static MemoryUnlinked MemoryUnlink(string tenant, string memoryReferenceId)
        => new(
            tenant,
            ProjectIdValue,
            memoryReferenceId,
            "actor-001",
            "corr-memory",
            "task-memory",
            "idem-key-unlink-" + memoryReferenceId,
            "sha256:unlink-" + memoryReferenceId,
            DateTimeOffset.UnixEpoch.AddMinutes(8));

    private sealed record UnknownProjectEvent : IProjectEvent
    {
        public string TenantId => TenantA;

        public string ProjectId => ProjectIdValue;

        public string CorrelationId => "corr-001";

        public string TaskId => "task-001";

        public string IdempotencyKey => "idem-key-zzz";

        public string IdempotencyFingerprint => "sha256:feedface";

        public DateTimeOffset OccurredAt => DateTimeOffset.UnixEpoch;
    }
}
