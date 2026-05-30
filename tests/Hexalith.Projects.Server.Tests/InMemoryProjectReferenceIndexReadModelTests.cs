// <copyright file="InMemoryProjectReferenceIndexReadModelTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>
/// Story 4.3 Tier-1/Tier-2 coverage that drives the REAL reverse reference-index read-model chain
/// (<see cref="InMemoryProjectReferenceIndexReadModel"/> -&gt; ProjectReferenceIndexReadModelMapper -&gt;
/// ProjectReferenceIndexProjection/ProjectListProjection) instead of the endpoint test's hand-written
/// stub. This is the read-model coverage the story directed dev to author up front (the Story 4.2
/// "new ACL/read-model shipped untested" lesson).
/// </summary>
public sealed class InMemoryProjectReferenceIndexReadModelTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectAId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string ProjectBId = "01HZ9K8YQ3W6V2N4R7T5P0X1BB";
    private const string OrphanProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1CC";
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FileId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";

    [Fact]
    public async Task ListByReference_FolderMatch_ReturnsTenantCandidateWithLifecycleAndDisplayName()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectAId, "Apollo", ProjectLifecycle.Active));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectAId));

        var rows = await readModel
            .ListByReferenceAsync(TenantA, [FolderId], [], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectReferenceIndexCandidateRow row = rows.ShouldHaveSingleItem();
        row.TenantId.ShouldBe(TenantA);
        row.ProjectId.ShouldBe(ProjectAId);
        row.DisplayName.ShouldBe("Apollo");
        row.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        row.MatchedReferences.ShouldHaveSingleItem().ReferenceKind.ShouldBe("folder");
        row.MatchedReferences[0].ReferenceId.ShouldBe(FolderId);
        row.MatchedReferences[0].ReferenceState.ShouldBe(ReferenceState.Included);
    }

    [Fact]
    public async Task ListByReference_FileMatch_ReturnsCandidateAfterFolderAndFileEvents()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectAId, "Apollo", ProjectLifecycle.Active));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectAId));
        readModel.Project(TenantA, FileLinked(TenantA, ProjectAId, FileId));

        var rows = await readModel
            .ListByReferenceAsync(TenantA, [], [FileId], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectReferenceIndexCandidateRow row = rows.ShouldHaveSingleItem();
        row.ProjectId.ShouldBe(ProjectAId);
        row.MatchedReferences.ShouldHaveSingleItem().ReferenceKind.ShouldBe("file");
        row.MatchedReferences[0].ReferenceId.ShouldBe(FileId);
    }

    [Fact]
    public async Task ListByReference_ArchivedProject_CarriesArchivedLifecycle()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectAId, "Apollo", ProjectLifecycle.Active));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectAId));
        readModel.Project(TenantA, Archived(TenantA, ProjectAId));

        var rows = await readModel
            .ListByReferenceAsync(TenantA, [FolderId], [], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // The read model never applies the archived gate (that is the engine's job); it must surface the
        // real lifecycle so the engine can exclude it unless includeArchived is requested.
        rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(ProjectLifecycle.Archived);
    }

    [Fact]
    public async Task ListByReference_CrossTenantReference_IsDroppedByTenantFilter()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        // A folder reference that belongs ONLY to a tenant-B project.
        readModel.Project(TenantB, Created(TenantB, ProjectBId, "Beacon", ProjectLifecycle.Active));
        readModel.Project(TenantB, FolderSet(TenantB, ProjectBId));

        // Tenant A presents the same folder id — the real projection + ProjectQueryTenantFilter must
        // drop it (no cross-tenant candidate, no existence leak).
        var rows = await readModel
            .ListByReferenceAsync(TenantA, [FolderId], [], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListByReference_ReferenceWithoutListedProject_IsDroppedByJoin()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        // A file reference whose project never produced a ProjectCreated (so it is absent from the list
        // projection). The mapper's ContainsKey inner-join must drop it rather than emit a candidate
        // with a missing lifecycle/display name.
        readModel.Project(TenantA, FileLinked(TenantA, OrphanProjectId, FileId));

        var rows = await readModel
            .ListByReferenceAsync(TenantA, [], [FileId], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListByReference_TwoMatchingProjects_OrderedByProjectIdOrdinal()
    {
        InMemoryProjectReferenceIndexReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectBId, "Beacon", ProjectLifecycle.Active));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectBId));
        readModel.Project(TenantA, Created(TenantA, ProjectAId, "Apollo", ProjectLifecycle.Active));
        readModel.Project(TenantA, FileLinked(TenantA, ProjectAId, FileId));

        var rows = await readModel
            .ListByReferenceAsync(TenantA, [FolderId], [FileId], TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        rows.Select(row => row.ProjectId).ShouldBe([ProjectAId, ProjectBId]);
    }

    private static ProjectCreated Created(string tenant, string projectId, string name, ProjectLifecycle lifecycle)
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
            "idem-created-" + projectId,
            "sha256:created-" + projectId,
            DateTimeOffset.UnixEpoch);

    private static ProjectArchived Archived(string tenant, string projectId)
        => new(
            tenant,
            projectId,
            ProjectLifecycle.Archived,
            "actor-001",
            "corr-archive",
            "task-archive",
            "idem-archive-" + projectId,
            "sha256:archive-" + projectId,
            DateTimeOffset.UnixEpoch.AddMinutes(3));

    private static ProjectFolderSet FolderSet(string tenant, string projectId)
        => new(
            tenant,
            projectId,
            FolderId,
            new ProjectFolderMetadata("Tracer Folder"),
            "actor-001",
            "corr-folder",
            "task-folder",
            "idem-folder-" + projectId,
            "sha256:folder-" + projectId,
            DateTimeOffset.UnixEpoch.AddMinutes(1));

    private static FileReferenceLinked FileLinked(string tenant, string projectId, string fileReferenceId)
        => new(
            tenant,
            projectId,
            fileReferenceId,
            FolderId,
            new ProjectFileReferenceMetadata("contract.pdf"),
            "actor-001",
            "corr-file",
            "task-file",
            "idem-file-" + projectId,
            "sha256:file-" + projectId,
            DateTimeOffset.UnixEpoch.AddMinutes(2));
}
