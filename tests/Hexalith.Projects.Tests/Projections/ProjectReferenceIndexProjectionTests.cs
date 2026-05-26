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
