// <copyright file="ProjectResolutionEngineTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Testing.Resolution;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Story 4.1 epic-named resolution cases and core reason-code behavior.</summary>
public sealed class ProjectResolutionEngineTests
{
    [Fact]
    public void Resolve_NoMatch_ReturnsNoMatch()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), []);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_SingleCandidate_ReturnsSingleCandidateWithReasonCodes()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [Candidate(signals: [Signal(ProjectReasonCode.ConversationLinked), Signal(ProjectReasonCode.FileReferenceMatched, referenceId: "file-001", kind: "file")])]);

        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates.Count.ShouldBe(1);
        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Candidates[0].Rank.ShouldBe(1);
        result.Candidates[0].Score.ShouldBe(85);
        result.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.ConversationLinked, ProjectReasonCode.FileReferenceMatched]);
    }

    [Fact]
    public void Resolve_MultipleCandidates_ReturnsMultipleCandidates()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [
                Candidate("project-b", signals: [Signal(ProjectReasonCode.MetadataMatched, referenceId: "metadata-b", kind: "metadata")]),
                Candidate("project-a", signals: [Signal(ProjectReasonCode.ConversationLinked)]),
            ]);

        result.Result.ShouldBe(ResolutionResult.MultipleCandidates);
        result.Candidates.Select(static c => c.ProjectId).ShouldBe(["project-a", "project-b"]);
    }

    [Fact]
    public void Resolve_ArchivedExclusion_ExcludesArchivedByDefault()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(includeArchived: false),
            [Candidate(lifecycle: ProjectLifecycle.Archived)]);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.Count.ShouldBe(1);
        result.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Archived);
        result.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ProjectArchived);
    }

    [Fact]
    public void Resolve_ArchivedOptIn_AllowsArchivedCandidate()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(includeArchived: true),
            [Candidate(lifecycle: ProjectLifecycle.Archived)]);

        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_UnauthorizedResourceExclusion_FailsClosedWithoutDroppingEvidence()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [Candidate(signals: [Signal(ProjectReasonCode.MemoryMatched, ReferenceState.Unauthorized, "case-001", "memory")])]);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.Count.ShouldBe(1);
        result.Excluded[0].ProjectId.ShouldBe("project-a");
        result.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Unauthorized);
        result.Excluded[0].ReasonCode.ShouldBe(ProjectReasonCode.MemoryMatched);
        result.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ReferenceUnauthorized);
    }

    [Fact]
    public void Resolve_NonIncludedSignalOnQualifyingCandidate_IsSurfacedAsExclusion()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [
                Candidate(signals:
                [
                    Signal(ProjectReasonCode.ProjectFolderMatched, ReferenceState.Included, "folder-001", "folder"),
                    Signal(ProjectReasonCode.FileReferenceMatched, ReferenceState.Stale, "file-001", "file"),
                ]),
            ]);

        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.ProjectFolderMatched]);
        result.Excluded.Count.ShouldBe(1);
        result.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Stale);
        result.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ReferenceStale);
    }

    [Fact]
    public void Resolve_MissingTenantAuthority_FailsClosedForEveryCandidate()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(authoritativeTenantId: null, requestedTenantId: DefaultTenant),
            [Candidate("project-a"), Candidate("project-b")]);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.Count.ShouldBe(2);
        result.Excluded.Select(static e => e.ReferenceState).Distinct().ShouldBe([ReferenceState.TenantMismatch]);
        result.Excluded.Select(static e => e.Diagnostic).Distinct().ShouldBe([ProjectContextInclusionDiagnostic.TenantMismatch]);
    }
}
