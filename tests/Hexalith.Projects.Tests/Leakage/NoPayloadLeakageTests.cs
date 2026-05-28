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
    public void MemoryLinked_SerializesMetadataOnly()
    {
        MemoryLinked linked = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            new ProjectMemoryReferenceMetadata("Q3 product strategy memory"),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-memory-link",
            "sha256:memory-link",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(linked));
    }

    [Fact]
    public void MemoryUnlinked_SerializesMetadataOnly()
    {
        MemoryUnlinked unlinked = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            "actor-001",
            "corr-001",
            "task-001",
            "idem-memory-unlink",
            "sha256:memory-unlink",
            DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(unlinked));
    }

    [Fact]
    public void MemoryReferenceLinkRejection_SerializesMetadataOnly()
    {
        ProjectReferenceLinkRejected rejection = new(
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            "acme",
            "memory",
            "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            ReferenceState.Conflict,
            "memoryReferenceId",
            "corr-memory");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void MemoryReferenceUnlinkRejection_SerializesMetadataOnly()
    {
        ProjectReferenceUnlinkRejected rejection = new(
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            "acme",
            "memory",
            "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            ReferenceState.TenantMismatch,
            "memoryReferenceId",
            "corr-memory");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(rejection));
    }

    [Fact]
    public void LinkMemoryRejection_DropsUnsafeReferenceId()
    {
        LinkMemory command = new(
            "acme",
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            @"C:\Users\acme\secret",
            new ProjectMemoryReferenceMetadata("Q3 product strategy memory"),
            "actor-001",
            "corr-memory",
            "task-memory",
            "idem-memory");

        ProjectResult result = ProjectResult.Rejected(
            command,
            ProjectResultCode.ValidationFailed,
            nameof(LinkMemory.MemoryReferenceId));

        ProjectReferenceLinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceLinkRejected>();
        rejection.ReferenceKind.ShouldBe("memory");
        rejection.ReferenceId.ShouldBe("unknown");
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

    // === Story 3.1 (AC 10) NoPayloadLeakage extension over the new ProjectContext assembly DTOs ===

    [Fact]
    public void ProjectContextReference_SerializesMetadataOnly()
    {
        ProjectContextReference reference = new(
            ReferenceKind: "memory",
            ReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            DisplayName: "Q3 product strategy memory",
            ReferenceState: ReferenceState.Included,
            ReasonCode: ProjectReasonCode.MemoryMatched,
            ObservedAt: DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(reference));
    }

    [Fact]
    public void ProjectContextExclusion_SerializesMetadataOnly()
    {
        ProjectContextExclusion exclusion = new(
            ReferenceKind: "memory",
            ReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
            ReferenceState: ReferenceState.Unauthorized,
            ReasonCode: null,
            FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
            Diagnostic: ProjectContextInclusionDiagnostic.TenantMismatch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(exclusion));
    }

    [Fact]
    public void ProjectContextEvaluation_SerializesMetadataOnly()
    {
        ProjectContextEvaluation evaluation = new(
            ReferenceKind: "file",
            ReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1",
            ResultState: ReferenceState.Stale,
            FailedCheck: ProjectContextInclusionCheck.ReferenceFreshness,
            ReasonCode: null,
            Diagnostic: ProjectContextInclusionDiagnostic.ReferenceStale,
            ObservedAt: DateTimeOffset.UnixEpoch);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(evaluation));
    }

    [Fact]
    public void ProjectContext_HappyPath_SerializesMetadataOnly()
    {
        ProjectContext context = new(
            TenantId: "acme",
            ProjectId: "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            Lifecycle: ProjectLifecycle.Active,
            Setup: null,
            ProjectFolder: new ProjectContextReference(
                "folder", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC", "Tracer Folder",
                ReferenceState.Included, ProjectReasonCode.ProjectFolderMatched, DateTimeOffset.UnixEpoch),
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences:
            [
                new ProjectContextReference(
                    "file", "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1", "contract.pdf",
                    ReferenceState.Included, ProjectReasonCode.FileReferenceMatched, DateTimeOffset.UnixEpoch),
            ],
            MemoryReferences:
            [
                new ProjectContextReference(
                    "memory", "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1", "Q3 product strategy memory",
                    ReferenceState.Included, ProjectReasonCode.MemoryMatched, DateTimeOffset.UnixEpoch),
            ],
            Excluded: Array.Empty<ProjectContextExclusion>(),
            AssemblyOutcome: ProjectContextAssemblyOutcome.Assembled,
            ObservedAt: DateTimeOffset.UnixEpoch,
            Freshness: ProjectContextFreshness.Fresh);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(context));
    }

    [Fact]
    public void ProjectContext_ArchivedProjectWithExcludedRows_SerializesMetadataOnly()
    {
        ProjectContext context = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            ProjectLifecycle.Archived,
            Setup: null,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences: Array.Empty<ProjectContextReference>(),
            Excluded:
            [
                new ProjectContextExclusion(
                    "memory",
                    "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
                    ReferenceState.Archived,
                    null,
                    ProjectContextInclusionCheck.ProjectLifecycle,
                    ProjectContextInclusionDiagnostic.ProjectArchived),
            ],
            AssemblyOutcome: ProjectContextAssemblyOutcome.Assembled,
            ObservedAt: DateTimeOffset.UnixEpoch,
            Freshness: ProjectContextFreshness.Fresh);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(context));
    }

    [Fact]
    public void ProjectContextAssemblyResult_SerializesMetadataOnly()
    {
        ProjectContextAssemblyResult result = new(
            new ProjectContext(
                "acme",
                "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
                ProjectLifecycle.Active,
                Setup: null,
                ProjectFolder: null,
                Conversations: Array.Empty<ProjectContextReference>(),
                FileReferences: Array.Empty<ProjectContextReference>(),
                MemoryReferences: Array.Empty<ProjectContextReference>(),
                Excluded: Array.Empty<ProjectContextExclusion>(),
                AssemblyOutcome: ProjectContextAssemblyOutcome.Assembled,
                ObservedAt: DateTimeOffset.UnixEpoch,
                Freshness: ProjectContextFreshness.Fresh),
            Evaluations: Array.Empty<ProjectContextEvaluation>());

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result));
    }

    // Memories-specific forbidden-term coverage extended from Story 2.7. These payload-classification
    // terms must never appear in any assembled ProjectContext, even as field names.
    [Theory]
    [InlineData("Content")]
    [InlineData("ContentBytes")]
    [InlineData("ContentHash")]
    [InlineData("SourceUri")]
    [InlineData("SourceType")]
    [InlineData("IngestedBy")]
    [InlineData("EmbeddingProvider")]
    [InlineData("EmbeddingModel")]
    [InlineData("EmbeddingDimensions")]
    [InlineData("FailureDetails")]
    [InlineData("IngestionInput")]
    public void MemoriesSpecificForbiddenTerms_AreNeverPresentInAssembledContext(string term)
    {
        ProjectContext context = new(
            "acme",
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            ProjectLifecycle.Active,
            Setup: null,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences:
            [
                new ProjectContextReference(
                    "memory", "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1", "Q3 product strategy memory",
                    ReferenceState.Included, ProjectReasonCode.MemoryMatched, DateTimeOffset.UnixEpoch),
            ],
            Excluded: Array.Empty<ProjectContextExclusion>(),
            AssemblyOutcome: ProjectContextAssemblyOutcome.Assembled,
            ObservedAt: DateTimeOffset.UnixEpoch,
            Freshness: ProjectContextFreshness.Fresh);

        string serialized = System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        serialized.ShouldNotContain(term, Case.Sensitive);
    }

    [Fact]
    public void ClosedDiagnosticVocabulary_NeverIntroducesPayloadTaxonomyTerm()
    {
        foreach (string diagnostic in ProjectContextInclusionDiagnostic.Values)
        {
            foreach (string forbidden in PayloadClassification.ForbiddenContent)
            {
                diagnostic.ToLowerInvariant().ShouldNotContain(forbidden.ToLowerInvariant());
            }
        }
    }
}
