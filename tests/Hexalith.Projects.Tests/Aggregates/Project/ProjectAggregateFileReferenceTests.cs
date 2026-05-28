// <copyright file="ProjectAggregateFileReferenceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System;
using System.Linq;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for the optional File Reference handlers of <see cref="ProjectAggregate"/>
/// (Story 2.5, AC 2, 3, 4, 8). No Dapr, Aspire, network, containers, or browser.
/// </summary>
public sealed class ProjectAggregateFileReferenceTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FileReferenceId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string FolderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";

    [Fact]
    public void LinkFileReference_InitialReference_EmitsMetadataOnlyEvent()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.FileReferenceLinked);
        FileReferenceLinked linked = result.Events.Single().ShouldBeOfType<FileReferenceLinked>();
        linked.FileReferenceId.ShouldBe(FileReferenceId);
        linked.FolderId.ShouldBe(FolderId);
        linked.FileMetadata.DisplayName.ShouldBe("contract.pdf");
    }

    [Fact]
    public void LinkFileReference_DoesNotTouchProjectFolder()
    {
        ProjectState created = Created();
        ProjectState afterLink = ApplyLink(created, LinkCommand());

        // The Project Folder pending state from creation is untouched by a file link.
        afterLink.ProjectFolder.ShouldBe(created.ProjectFolder);
        afterLink.FileReferences.Count.ShouldBe(1);
    }

    [Fact]
    public void LinkFileReference_DuplicateEquivalentDifferentKey_IsIdempotentReplay()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withFile, LinkCommand(idempotencyKey: "idem-file-002"));

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void LinkFileReference_SameReferenceConflictingMetadata_IsConflict()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withFile,
            LinkCommand(folderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ", idempotencyKey: "idem-file-002"));

        result.Code.ShouldBe(ProjectResultCode.FileReferenceConflict);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void LinkFileReference_SameKeySamePayload_IsIdempotentReplay()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withFile, LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
    }

    [Fact]
    public void LinkFileReference_SameKeyDifferentPayload_IsIdempotencyConflict()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withFile,
            LinkCommand(fileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F2"));

        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
    }

    [Fact]
    public void LinkFileReference_MultipleDistinctReferences_AreAllStored()
    {
        ProjectState withFirst = ApplyLink(Created(), LinkCommand());
        ProjectState withSecond = ApplyLink(
            withFirst,
            LinkCommand(fileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F2", idempotencyKey: "idem-file-002"));

        withSecond.FileReferences.Count.ShouldBe(2);
        withSecond.FileReferences.ShouldContainKey(FileReferenceId);
        withSecond.FileReferences.ShouldContainKey("file_01HZ9K8YQ3W6V2N4R7T5P0X1F2");
    }

    [Fact]
    public void LinkFileReference_BoundedSetExceeded_IsRejected()
    {
        ProjectState state = Created();
        for (int i = 0; i < ProjectState.MaxFileReferences; i++)
        {
            state = ApplyLink(state, LinkCommand(fileReferenceId: $"file_{i:D26}", idempotencyKey: $"idem-{i:D4}"));
        }

        ProjectResult result = ProjectAggregate.Handle(
            state,
            LinkCommand(fileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F9", idempotencyKey: "idem-over"));

        result.Code.ShouldBe(ProjectResultCode.FileReferenceLimitExceeded);
    }

    [Fact]
    public void LinkFileReference_ProjectNotCreated_IsProjectNotFound()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectNotFound);
    }

    [Fact]
    public void LinkFileReference_TenantMismatch_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand() with { TenantId = "other-tenant" });

        result.Code.ShouldBe(ProjectResultCode.TenantMismatch);
    }

    [Fact]
    public void LinkFileReference_ArchivedProject_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("bad id")]
    [InlineData("")]
    public void LinkFileReference_MalformedFileReferenceId_IsValidationFailed(string fileReferenceId)
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand(fileReferenceId: fileReferenceId));

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(LinkFileReference.FileReferenceId));
    }

    [Fact]
    public void LinkFileReference_MalformedFolderId_IsValidationFailed()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand(folderId: "C:/folders/x"));

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(LinkFileReference.FolderId));
    }

    [Fact]
    public void UnlinkFileReference_ExistingReference_EmitsMetadataOnlyEvent()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withFile, UnlinkCommand());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.FileReferenceUnlinked);
        result.Events.Single().ShouldBeOfType<FileReferenceUnlinked>().FileReferenceId.ShouldBe(FileReferenceId);
    }

    [Fact]
    public void UnlinkFileReference_MissingReference_IsIdempotentNoOp()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void UnlinkFileReference_RemovesOnlyTargetedReference()
    {
        ProjectState withTwo = ApplyLink(
            ApplyLink(Created(), LinkCommand()),
            LinkCommand(fileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F2", idempotencyKey: "idem-file-002"));

        ProjectState afterUnlink = ApplyUnlink(withTwo, UnlinkCommand(idempotencyKey: "idem-unlink-001"));

        afterUnlink.FileReferences.ShouldNotContainKey(FileReferenceId);
        afterUnlink.FileReferences.ShouldContainKey("file_01HZ9K8YQ3W6V2N4R7T5P0X1F2");
    }

    [Fact]
    public void UnlinkFileReference_ProjectNotCreated_IsProjectNotFound()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectNotFound);
    }

    [Fact]
    public void UnlinkFileReference_TenantMismatch_IsRejected()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withFile, UnlinkCommand() with { TenantId = "other-tenant" });

        result.Code.ShouldBe(ProjectResultCode.TenantMismatch);
    }

    [Fact]
    public void UnlinkFileReference_ArchivedProject_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
    }

    [Fact]
    public void UnlinkFileReference_MalformedFileReferenceId_IsValidationFailed()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), UnlinkCommand(fileReferenceId: "../x"));

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(UnlinkFileReference.FileReferenceId));
    }

    [Fact]
    public void LinkRejection_MapsToFileReferenceLinkRejectedWithFileKind()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), LinkCommand());

        ProjectReferenceLinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceLinkRejected>();
        rejection.ReferenceKind.ShouldBe("file");
        rejection.ReferenceId.ShouldBe(FileReferenceId);
        rejection.Reason.ShouldBe(ReferenceState.Archived);
    }

    [Fact]
    public void UnlinkRejection_MapsToFileReferenceUnlinkRejectedWithFileKind()
    {
        ProjectState withFile = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withFile, UnlinkCommand() with { TenantId = "other-tenant" });

        ProjectReferenceUnlinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceUnlinkRejected>();
        rejection.ReferenceKind.ShouldBe("file");
        rejection.ReferenceId.ShouldBe(FileReferenceId);
        rejection.Reason.ShouldBe(ReferenceState.TenantMismatch);
    }

    private static LinkFileReference LinkCommand(
        string fileReferenceId = FileReferenceId,
        string folderId = FolderId,
        string? displayName = "contract.pdf",
        string idempotencyKey = "idem-file-001") => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        fileReferenceId,
        folderId,
        new ProjectFileReferenceMetadata(displayName),
        "actor-001",
        "corr-001",
        "task-001",
        idempotencyKey);

    private static UnlinkFileReference UnlinkCommand(
        string fileReferenceId = FileReferenceId,
        string idempotencyKey = "idem-unlink-001") => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        fileReferenceId,
        "actor-001",
        "corr-001",
        "task-001",
        idempotencyKey);

    private static ProjectState Created()
    {
        CreateProject create = new(
            Tenant,
            new ProjectId(ProjectIdValue),
            "Tracer Bullet",
            null,
            null,
            "actor-001",
            "corr-001",
            "task-001",
            "idem-create-001");
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, create);
        ProjectIdentity identity = new(create.TenantId, create.ProjectId);
        return ProjectState.Empty.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState Archived()
    {
        ProjectState created = Created();
        ArchiveProject archive = new(
            Tenant,
            new ProjectId(ProjectIdValue),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-archive-001");
        ProjectResult accepted = ProjectAggregate.Handle(created, archive);
        ProjectIdentity identity = new(archive.TenantId, archive.ProjectId);
        return created.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState ApplyLink(ProjectState state, LinkFileReference command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(state, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState ApplyUnlink(ProjectState state, UnlinkFileReference command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(state, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }
}
