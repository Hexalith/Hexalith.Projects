// <copyright file="NoPayloadLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Leakage;

using System;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;

/// <summary>
/// FS-2 <c>NoPayloadLeakage</c> harness tests (AC 4): the reusable
/// <see cref="NoPayloadLeakageAssertions"/> guard asserts the success and rejection events serialize
/// metadata-only, and the harness itself detects a forbidden category when one is injected.
/// </summary>
public sealed class NoPayloadLeakageTests
{
    [Fact]
    public void ProjectCreated_SerializesMetadataOnly()
    {
        ProjectCreated created = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "Tracer Bullet",
            "A safe description",
            null,
            ProjectLifecycle.Active,
            "actor-001",
            "corr-001",
            "task-001",
            "idem-key-001",
            "sha256:deadbeef",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(created));
    }

    [Fact]
    public void ProjectCreationRejected_SerializesMetadataOnly()
    {
        ProjectCreationRejected rejection = new(
            "acme",
            ReferenceState.Unauthorized,
            "SetupMetadata",
            "corr-001",
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"));

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void ProjectSetupUpdated_SerializesMetadataOnly()
    {
        ProjectSetupUpdated updated = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            new ProjectSetup(
                ["keep continuity current"],
                ["use safe project references"],
                [ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory],
                [ProjectContextSourceKind.FileReference],
                new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences)),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-key-setup",
            "sha256:setup",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(updated));
    }

    [Fact]
    public void ProjectArchived_SerializesMetadataOnly()
    {
        ProjectArchived archived = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            ProjectLifecycle.Archived,
            "actor-001",
            "corr-001",
            "task-001",
            "idem-key-archive",
            "sha256:archive",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(archived));
    }

    [Fact]
    public void ProjectFolderCreationPending_SerializesMetadataOnly()
    {
        ProjectFolderCreationPending pending = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "Tracer Bullet",
            "folder_create_external_unavailable",
            true,
            "actor-001",
            "corr-001",
            "task-001",
            "idem-folder-pending",
            "sha256:folder-pending",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(pending));
    }

    [Fact]
    public void ProjectFolderSet_SerializesMetadataOnly()
    {
        ProjectFolderSet folderSet = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            new ProjectFolderMetadata("Tracer Folder"),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-folder-set",
            "sha256:folder-set",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(folderSet));
    }

    [Fact]
    public void FileReferenceLinked_SerializesMetadataOnly()
    {
        FileReferenceLinked linked = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1",
            "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            new ProjectFileReferenceMetadata("contract.pdf"),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-file-link",
            "sha256:file-link",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(linked));
    }

    [Fact]
    public void FileReferenceUnlinked_SerializesMetadataOnly()
    {
        FileReferenceUnlinked unlinked = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1",
            "actor-001",
            "corr-001",
            "task-001",
            "idem-file-unlink",
            "sha256:file-unlink",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(unlinked));
    }

    [Fact]
    public void FileReferenceUnlinkRejection_SerializesMetadataOnly()
    {
        ProjectReferenceUnlinkRejected rejection = new(
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            "acme",
            "file",
            "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1",
            ReferenceState.TenantMismatch,
            "fileReferenceId",
            "corr-file");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void LinkFileReferenceRejection_DropsUnsafeReferenceId()
    {
        LinkFileReference command = new(
            "acme",
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            @"C:\Users\acme\secret.txt",
            "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            new ProjectFileReferenceMetadata("contract.pdf"),
            "actor-001",
            "corr-file",
            "task-file",
            "idem-file");

        ProjectResult result = ProjectResult.Rejected(
            command,
            ProjectResultCode.ValidationFailed,
            nameof(LinkFileReference.FileReferenceId));

        ProjectReferenceLinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceLinkRejected>();
        rejection.ReferenceKind.ShouldBe("file");
        rejection.ReferenceId.ShouldBe("unknown");
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void SetupAndArchiveRejections_SerializeMetadataOnly()
    {
        var projectId = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(
            new ProjectSetupUpdateRejected(projectId, "acme", ReferenceState.InvalidReference, "setup.goals", "corr-setup")));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(
            new ProjectArchiveRejected(projectId, "acme", ReferenceState.Archived, "lifecycle", "corr-archive")));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(
            new ProjectReferenceLinkRejected(projectId, "acme", "folder", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC", ReferenceState.Unavailable, "folderId", "corr-folder")));
    }

    [Fact]
    public void SetProjectFolderRejection_DropsUnsafeReferenceId()
    {
        SetProjectFolder command = new(
            "acme",
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            @"C:\Users\acme\secret.txt",
            new ProjectFolderMetadata("Tracer Folder"),
            false,
            "actor-001",
            "corr-folder",
            "task-folder",
            "idem-folder");

        ProjectResult result = ProjectResult.Rejected(
            command,
            ProjectResultCode.ValidationFailed,
            nameof(SetProjectFolder.FolderId));

        ProjectReferenceLinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceLinkRejected>();
        rejection.ReferenceId.ShouldBe("unknown");
        rejection.RejectedField.ShouldBe(nameof(SetProjectFolder.FolderId));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void TenantAccessProjection_SerializesMetadataOnly()
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = "acme",
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = "acme:1",
            LastEventTimestamp = DateTimeOffset.UnixEpoch,
        };
        projection.Principals["actor-001"] = new ProjectTenantPrincipalEvidence("actor-001", "TenantOwner");
        projection.ProcessedMessages["msg-001"] = new ProjectTenantEventEvidence(
            "msg-001",
            "acme",
            nameof(ProjectTenantAccessEventKind.UserAddedToTenant),
            1,
            DateTimeOffset.UnixEpoch,
            "sha256:tenant-access-metadata");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(projection));
    }

    [Fact]
    public void ProjectTenantAccessEvent_SerializesMetadataOnly()
    {
        ProjectTenantAccessEvent @event = new(
            ProjectTenantAccessEventKind.UserAddedToTenant,
            "acme",
            "msg-001",
            1,
            DateTimeOffset.UnixEpoch,
            "corr-001",
            "actor-001",
            "TenantOwner",
            null,
            "projects.create.enabled",
            "sha256:tenant-access-metadata");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(@event));
    }

    [Fact]
    public void ProjectConversationReferences_SerializeMetadataOnly()
    {
        ProjectId projectId = new("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        ProjectConversationsPage page = new(
            projectId,
            [
                new ProjectConversationItem(
                    projectId,
                    new ConversationId("01HZ9K8YQ3W6V2N4R7T5P0X1AC"),
                    "Open",
                    "Synthetic conversation reference",
                    ProjectConversationTrustSignal.Current,
                    "Synthetic project",
                    "resolved"),
            ],
            new ProjectConversationPageMetadata(1, "cursor-001"),
            ProjectConversationTrustSignal.Current);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(page));
    }

    [Fact]
    public void TenantAccessAuthorizationResult_SerializesMetadataOnly()
    {
        TenantAccessAuthorizationResult result = new(
            TenantAccessOutcome.StaleProjection,
            "stale_projection",
            "acme",
            "acme:7",
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromMinutes(20),
            TenantProjectionFreshnessStatus.Stale,
            "local-projection");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result));
    }

    [Fact]
    public void ProjectCreated_LogScopeRendering_IsMetadataOnly()
    {
        // A representative structured log scope: ids/reason codes/correlation/freshness only.
        string logScope = "tenant=acme project=01HZ9K8YQ3W6V2N4R7T5P0X1AB lifecycle=Active correlation=corr-001 occurredAt=1970-01-01T00:00:00Z";
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(logScope));
    }

    [Theory]
    [InlineData("{\"fileContents\":\"...\"}")]
    [InlineData("{\"value\":\"/home/user/secret\"}")]
    [InlineData("{\"token\":\"eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.abcDEFghiJKL\"}")]
    [InlineData("-----BEGIN RSA PRIVATE KEY-----")]
    public void Harness_DetectsForbiddenContent(string leaky)
    {
        Should.Throw<PayloadLeakageException>(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(leaky));
    }
}
