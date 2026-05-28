// <copyright file="ProjectReferenceIndexProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Projections;

using System;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

using Shouldly;

using Xunit;

/// <summary>Tier-1 tests for the metadata-only Project reference index.</summary>
public sealed class ProjectReferenceIndexProjectionTests
{
    private const string Tenant = "tenant-a";
    private const string ProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";

    [Fact]
    public void PendingFolder_IsIndexedAsPendingWithoutReferenceId()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Pending()),
        ]);

        ProjectReferenceIndexItem item = projection.List(Tenant, ProjectId).Single();
        item.ReferenceKind.ShouldBe("folder");
        item.ReferenceId.ShouldBeNull();
        item.ReferenceState.ShouldBe(ReferenceState.Pending);
        item.ReasonCode.ShouldBe("folder_create_external_unavailable");
    }

    [Fact]
    public void FolderSet_ReplacesPendingFolderIndex()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Pending()),
            new ProjectProjectionEnvelope(Tenant, 2, Set()),
        ]);

        ProjectReferenceIndexItem item = projection.List(Tenant, ProjectId).Single();
        item.ReferenceId.ShouldBe(FolderId);
        item.ReferenceState.ShouldBe(ReferenceState.Included);
        item.DisplayName.ShouldBe("Tracer Folder");
    }

    [Fact]
    public void FileReferenceLinked_IsIndexedAsIncludedFileRow()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
        ]);

        ProjectReferenceIndexItem file = projection.List(Tenant, ProjectId)
            .Single(item => item.ReferenceKind == "file");
        file.ReferenceId.ShouldBe("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1");
        file.ReferenceState.ShouldBe(ReferenceState.Included);
        file.DisplayName.ShouldBe("contract.pdf");
    }

    [Fact]
    public void FileReferenceUnlinked_RemovesOnlyFileRowAndKeepsFolderRow()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 3, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F2")),
            new ProjectProjectionEnvelope(Tenant, 4, Unlinked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "folder").ShouldBe(1);
        rows.Where(item => item.ReferenceKind == "file").Select(item => item.ReferenceId)
            .ShouldBe(["file_01HZ9K8YQ3W6V2N4R7T5P0X1F2"]);
    }

    [Fact]
    public void FolderReplacement_KeepsFileRows()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 2, Set()),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "file").ShouldBe(1);
        rows.Single(item => item.ReferenceKind == "folder").ReferenceId.ShouldBe(FolderId);
    }

    [Fact]
    public void List_OrdersByReferenceKindThenReferenceId()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F2")),
            new ProjectProjectionEnvelope(Tenant, 3, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Select(item => item.ReferenceKind).ShouldBe(["file", "file", "folder"]);
        rows.Where(item => item.ReferenceKind == "file").Select(item => item.ReferenceId)
            .ShouldBe(["file_01HZ9K8YQ3W6V2N4R7T5P0X1F1", "file_01HZ9K8YQ3W6V2N4R7T5P0X1F2"]);
    }

    [Fact]
    public void MemoryLinked_IsIndexedAsIncludedMemoryRowOnDisjointLane()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 3, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
        ]);

        ProjectReferenceIndexItem memory = projection.List(Tenant, ProjectId)
            .Single(item => item.ReferenceKind == "memory");
        memory.ReferenceId.ShouldBe("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1");
        memory.ReferenceState.ShouldBe(ReferenceState.Included);
        memory.DisplayName.ShouldBe("Q3 product strategy memory");

        // Folder and file lanes are untouched by the memory link.
        projection.List(Tenant, ProjectId).Count(item => item.ReferenceKind == "folder").ShouldBe(1);
        projection.List(Tenant, ProjectId).Count(item => item.ReferenceKind == "file").ShouldBe(1);
    }

    [Fact]
    public void MemoryUnlinked_RemovesOnlyMemoryRowAndKeepsFolderAndFileRows()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 3, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
            new ProjectProjectionEnvelope(Tenant, 4, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M2")),
            new ProjectProjectionEnvelope(Tenant, 5, MemUnlinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "folder").ShouldBe(1);
        rows.Count(item => item.ReferenceKind == "file").ShouldBe(1);
        rows.Where(item => item.ReferenceKind == "memory").Select(item => item.ReferenceId)
            .ShouldBe(["case_01HZ9K8YQ3W6V2N4R7T5P0X1M2"]);
    }

    [Fact]
    public void FileUnlink_DoesNotTouchMemoryRows()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 3, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
            new ProjectProjectionEnvelope(Tenant, 4, Unlinked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "memory").ShouldBe(1);
        rows.Count(item => item.ReferenceKind == "file").ShouldBe(0);
        rows.Count(item => item.ReferenceKind == "folder").ShouldBe(1);
    }

    [Fact]
    public void FolderReplacement_DoesNotTouchMemoryRows()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
            new ProjectProjectionEnvelope(Tenant, 2, Set()),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "memory").ShouldBe(1);
        rows.Single(item => item.ReferenceKind == "folder").ReferenceId.ShouldBe(FolderId);
    }

    [Fact]
    public void MemoryUnlink_DoesNotTouchFolderOrFileRows()
    {
        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(
        [
            new ProjectProjectionEnvelope(Tenant, 1, Set()),
            new ProjectProjectionEnvelope(Tenant, 2, Linked("file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")),
            new ProjectProjectionEnvelope(Tenant, 3, MemLinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
            new ProjectProjectionEnvelope(Tenant, 4, MemUnlinked("case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")),
        ]);

        var rows = projection.List(Tenant, ProjectId);
        rows.Count(item => item.ReferenceKind == "memory").ShouldBe(0);
        rows.Count(item => item.ReferenceKind == "file").ShouldBe(1);
        rows.Single(item => item.ReferenceKind == "folder").ReferenceId.ShouldBe(FolderId);
    }

    private static FileReferenceLinked Linked(string fileReferenceId) => new(
        Tenant,
        ProjectId,
        fileReferenceId,
        FolderId,
        new ProjectFileReferenceMetadata("contract.pdf"),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-" + fileReferenceId,
        "sha256:" + fileReferenceId,
        DateTimeOffset.UnixEpoch.AddMinutes(2));

    private static FileReferenceUnlinked Unlinked(string fileReferenceId) => new(
        Tenant,
        ProjectId,
        fileReferenceId,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-unlink-" + fileReferenceId,
        "sha256:unlink-" + fileReferenceId,
        DateTimeOffset.UnixEpoch.AddMinutes(3));

    private static MemoryLinked MemLinked(string memoryReferenceId) => new(
        Tenant,
        ProjectId,
        memoryReferenceId,
        new ProjectMemoryReferenceMetadata("Q3 product strategy memory"),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-" + memoryReferenceId,
        "sha256:" + memoryReferenceId,
        DateTimeOffset.UnixEpoch.AddMinutes(4));

    private static MemoryUnlinked MemUnlinked(string memoryReferenceId) => new(
        Tenant,
        ProjectId,
        memoryReferenceId,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-unlink-" + memoryReferenceId,
        "sha256:unlink-" + memoryReferenceId,
        DateTimeOffset.UnixEpoch.AddMinutes(5));

    private static ProjectFolderCreationPending Pending() => new(
        Tenant,
        ProjectId,
        "Tracer Folder",
        "folder_create_external_unavailable",
        true,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-folder-pending",
        "sha256:folder-pending",
        DateTimeOffset.UnixEpoch);

    private static ProjectFolderSet Set() => new(
        Tenant,
        ProjectId,
        FolderId,
        new ProjectFolderMetadata("Tracer Folder"),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-folder-set",
        "sha256:folder-set",
        DateTimeOffset.UnixEpoch.AddMinutes(1));
}
