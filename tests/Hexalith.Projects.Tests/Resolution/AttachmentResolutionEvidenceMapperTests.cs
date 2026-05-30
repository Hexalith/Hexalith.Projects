// <copyright file="AttachmentResolutionEvidenceMapperTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Testing.Context;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Story 4.3 Tier-1 tests for the pure attachment evidence mapper.</summary>
public sealed class AttachmentResolutionEvidenceMapperTests
{
    private const string FolderId = "folder-001";
    private const string FileId = "file-001";

    private static AttachmentResolutionMetadata Attachments(
        IReadOnlyList<string>? folderIds = null,
        IReadOnlyList<string>? fileIds = null)
        => new(
            (folderIds ?? [FolderId]).Select(static id => Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, id)).ToArray(),
            (fileIds ?? []).Select(static id => Reference(AttachmentResolutionEvidenceMapper.FileReferenceKind, id)).ToArray());

    private static AttachmentResolutionProjectCandidate Project(
        string projectId,
        IReadOnlyList<AttachmentResolutionReference>? folders = null,
        IReadOnlyList<AttachmentResolutionReference>? files = null,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active)
        => new(projectId, "Project " + projectId, lifecycle, folders ?? [], files ?? []);

    private static AttachmentResolutionReference Reference(
        string kind,
        string id,
        ReferenceState state = ReferenceState.Included)
        => new(kind, id, state);

    [Fact]
    public void Map_NoReferencedProject_YieldsNoEvidence_EngineReturnsNoMatch()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(),
            [Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, "other-folder")])],
            DefaultNow);

        evidence.ShouldBeEmpty();

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("folder")]
    [InlineData("file")]
    public void Map_SingleAttachmentMatch_EngineReturnsSingleCandidate(string kind)
    {
        AttachmentResolutionMetadata attachments = kind == "folder"
            ? Attachments(folderIds: [FolderId], fileIds: [])
            : Attachments(folderIds: [], fileIds: [FileId]);
        AttachmentResolutionProjectCandidate project = kind == "folder"
            ? Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)])
            : Project("project-a", files: [Reference(AttachmentResolutionEvidenceMapper.FileReferenceKind, FileId)]);
        ProjectReasonCode expected = kind == "folder"
            ? ProjectReasonCode.ProjectFolderMatched
            : ProjectReasonCode.FileReferenceMatched;

        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            attachments,
            [project],
            DefaultNow);

        evidence.Count.ShouldBe(1);
        evidence[0].Signals[0].ReasonCode.ShouldBe(expected);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates[0].ReasonCodes.ShouldBe([expected]);
    }

    [Fact]
    public void Map_TwoProjectsQualify_EngineReturnsMultipleCandidates()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(folderIds: [FolderId], fileIds: [FileId]),
            [
                Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)]),
                Project("project-b", files: [Reference(AttachmentResolutionEvidenceMapper.FileReferenceKind, FileId)]),
            ],
            DefaultNow);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);

        result.Result.ShouldBe(ResolutionResult.MultipleCandidates);
        result.Candidates.Count.ShouldBe(2);
        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Candidates[0].Score.ShouldBe(45);
        result.Candidates[1].Score.ShouldBe(35);
    }

    [Fact]
    public void Map_ProjectMatchedByFolderAndFile_AccumulatesBothReasonCodes()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(folderIds: [FolderId], fileIds: [FileId]),
            [
                Project(
                    "project-a",
                    folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)],
                    files: [Reference(AttachmentResolutionEvidenceMapper.FileReferenceKind, FileId)]),
            ],
            DefaultNow);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);

        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates[0].Score.ShouldBe(80);
        result.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.ProjectFolderMatched, ProjectReasonCode.FileReferenceMatched]);
    }

    [Fact]
    public void Map_ArchivedCandidate_ExcludedByDefault_ButIncludedWhenRequested()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(),
            [Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)], lifecycle: ProjectLifecycle.Archived)],
            DefaultNow);

        ProjectResolution excludedByDefault = new ProjectResolutionEngine().Resolve(Context(includeArchived: false), evidence);
        excludedByDefault.Result.ShouldBe(ResolutionResult.NoMatch);
        excludedByDefault.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Archived);

        ProjectResolution includedWhenRequested = new ProjectResolutionEngine().Resolve(Context(includeArchived: true), evidence);
        includedWhenRequested.Result.ShouldBe(ResolutionResult.SingleCandidate);
    }

    [Theory]
    [InlineData(ReferenceState.Stale, ProjectContextInclusionDiagnostic.ReferenceStale)]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionDiagnostic.ReferenceUnauthorized)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionDiagnostic.ReferenceUnavailable)]
    [InlineData(ReferenceState.Pending, ProjectContextInclusionDiagnostic.ProjectFolderPending)]
    public void Map_DegradedReferenceState_BecomesExclusionNotMatch(ReferenceState state, string diagnostic)
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(),
            [Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId, state)])],
            DefaultNow);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded[0].ReasonCode.ShouldBe(ProjectReasonCode.ProjectFolderMatched);
        result.Excluded[0].Diagnostic.ShouldBe(diagnostic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-other")]
    public void Map_ThenEngine_TenantFailClosed_ReturnsNoMatchWithStructuredWarning(string? requestedTenant)
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(),
            [Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)])],
            DefaultNow);
        RecordingLogger<ProjectResolutionEngine> logger = new();
        ProjectResolutionContext context = requestedTenant is null
            ? Context(authoritativeTenantId: null, requestedTenantId: null)
            : Context(authoritativeTenantId: DefaultTenant, requestedTenantId: requestedTenant);

        ProjectResolution result = new ProjectResolutionEngine(logger).Resolve(context, evidence);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Excluded.ShouldContain(e => e.ReferenceState == ReferenceState.TenantMismatch);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void Map_ThenEngine_ResultLeaksNoForbiddenContentOrTenant()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = AttachmentResolutionEvidenceMapper.Map(
            Attachments(),
            [Project("project-a", folders: [Reference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, FolderId)])],
            DefaultNow);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(result));
        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("tenantId");
        serialized.ShouldNotContain(DefaultTenant);
    }
}
