// <copyright file="InMemoryProjectAuditTimelineReadModelTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Text.Json;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>Tier-1-style tests for the server-facing Project audit timeline read model.</summary>
public sealed class InMemoryProjectAuditTimelineReadModelTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string ProjectA = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string ProjectB = "01HZ9K8YQ3W6V2N4R7T5P0X1BB";
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListAsync_FiltersByAuthoritativeTenantProjectAndLimit()
    {
        InMemoryProjectAuditTimelineReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectA, "Project A"));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectA));
        readModel.Project(TenantA, Created(TenantA, ProjectB, "Project B"));
        readModel.Project(TenantB, Created(TenantB, "01HZ9K8YQ3W6V2N4R7T5P0X1CC", "Project C"));

        IReadOnlyList<ProjectAuditTimelineItem> projectRows = await readModel
            .ListAsync(TenantA, ProjectA, limit: 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        IReadOnlyList<ProjectAuditTimelineItem> tenantRows = await readModel
            .ListAsync(TenantA, projectId: null, limit: null, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        projectRows.Select(static row => row.OperationType).ShouldBe(["project.folder_set"]);
        projectRows.ShouldAllBe(static row => row.TenantId == TenantA);
        projectRows.ShouldAllBe(static row => row.ProjectId == ProjectA);
        tenantRows.Select(static row => row.ProjectId).ShouldBe([ProjectB, ProjectA, ProjectA]);
    }

    [Fact]
    public async Task ListAsync_DropsDispatchTenantMismatchBeforeReturningRows()
    {
        InMemoryProjectAuditTimelineReadModel readModel = new();
        readModel.Project(TenantB, Created(TenantA, ProjectA, "Project A"));

        IReadOnlyList<ProjectAuditTimelineItem> tenantARows = await readModel
            .ListAsync(TenantA, projectId: null, limit: null, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        IReadOnlyList<ProjectAuditTimelineItem> tenantBRows = await readModel
            .ListAsync(TenantB, projectId: null, limit: null, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        tenantARows.ShouldBeEmpty();
        tenantBRows.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_DoesNotExposeFolderMetadataPayload()
    {
        InMemoryProjectAuditTimelineReadModel readModel = new();
        readModel.Project(TenantA, Created(TenantA, ProjectA, "Project A"));
        readModel.Project(TenantA, FolderSet(TenantA, ProjectA, "Sensitive Folder /local/path password=secret"));

        IReadOnlyList<ProjectAuditTimelineItem> rows = await readModel
            .ListAsync(TenantA, ProjectA, limit: null, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectAuditTimelineItem folderRow = rows.Single(static row => row.OperationType == "project.folder_set");
        folderRow.ReferenceKind.ShouldBe("folder");
        folderRow.ReferenceId.ShouldBe(FolderId);

        string serialized = JsonSerializer.Serialize(rows);
        serialized.ShouldNotContain("Sensitive Folder", Case.Insensitive);
        serialized.ShouldNotContain("/local/path", Case.Insensitive);
        serialized.ShouldNotContain("password=secret", Case.Insensitive);
    }

    private static ProjectCreated Created(string tenantId, string projectId, string name)
        => new(
            tenantId,
            projectId,
            name,
            "Synthetic description",
            null,
            ProjectLifecycle.Active,
            "principal-a",
            "corr-created-" + projectId,
            "task-created-" + projectId,
            "idem-created-" + projectId,
            "sha256:created-" + projectId,
            CreatedAt);

    private static ProjectFolderSet FolderSet(string tenantId, string projectId, string folderName = "Folder A")
        => new(
            tenantId,
            projectId,
            FolderId,
            new ProjectFolderMetadata(folderName),
            "principal-a",
            "corr-folder-" + projectId,
            "task-folder-" + projectId,
            "idem-folder-" + projectId,
            "sha256:folder-" + projectId,
            CreatedAt.AddMinutes(1));
}
