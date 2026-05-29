// <copyright file="ConversationResolutionEvidenceMapperTests.cs" company="Hexalith">
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

/// <summary>
/// Story 4.2 Tier-1 tests for the pure conversation→Projects evidence mapper and its composition with
/// the Story 4.1 <see cref="ProjectResolutionEngine"/>. Proves the mapper produces only
/// <see cref="ProjectReasonCode.ConversationLinked"/> / <see cref="ProjectReasonCode.MetadataMatched"/>
/// signals, fails closed on degraded conversation trust, and never duplicates engine scoring.
/// </summary>
public sealed class ConversationResolutionEvidenceMapperTests
{
    private const string ConversationId = "conversation-001";

    private static ConversationResolutionMetadata Conversation(
        string? linkedProjectId = null,
        string? safeLabel = null,
        ReferenceState referenceState = ReferenceState.Included)
        => new(ConversationId, linkedProjectId, safeLabel, referenceState);

    private static ConversationResolutionProjectCandidate Project(
        string projectId,
        string? displayName = null,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active)
        => new(projectId, displayName, lifecycle);

    [Fact]
    public void Map_NoQualifyingSignal_YieldsNoEvidence_EngineReturnsNoMatch()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: null, safeLabel: "Unrelated label"),
            [Project("project-a", "Project Alpha"), Project("project-b", "Project Beta")],
            DefaultNow);

        evidence.ShouldBeEmpty();

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.ShouldBeEmpty();
    }

    [Fact]
    public void Map_SingleConversationLinked_EngineReturnsSingleCandidate()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a"),
            [Project("project-a", "Project Alpha"), Project("project-b", "Project Beta")],
            DefaultNow);

        evidence.Count.ShouldBe(1);
        evidence[0].ProjectId.ShouldBe("project-a");
        evidence[0].Signals[0].ReasonCode.ShouldBe(ProjectReasonCode.ConversationLinked);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates.Count.ShouldBe(1);
        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Candidates[0].ReasonCodes.ShouldContain(ProjectReasonCode.ConversationLinked);
    }

    [Fact]
    public void Map_TwoQualifyingCandidates_EngineReturnsMultipleCandidates()
    {
        // project-a qualifies via ConversationLinked (50); project-b qualifies via MetadataMatched (20).
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a", safeLabel: "Project Beta"),
            [Project("project-a", "Project Alpha"), Project("project-b", "Project Beta")],
            DefaultNow);

        evidence.Count.ShouldBe(2);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.MultipleCandidates);
        result.Candidates.Count.ShouldBe(2);
        // Deterministic ranking: ConversationLinked (50) outranks MetadataMatched (20).
        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Candidates[1].ProjectId.ShouldBe("project-b");
        result.Candidates[1].ReasonCodes.ShouldContain(ProjectReasonCode.MetadataMatched);
    }

    [Fact]
    public void Map_MetadataMatchOnly_QualifiesViaMetadataMatchedReasonCode()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(safeLabel: "  project alpha  "),
            [Project("project-a", "Project Alpha")],
            DefaultNow);

        evidence.Count.ShouldBe(1);
        evidence[0].Signals[0].ReasonCode.ShouldBe(ProjectReasonCode.MetadataMatched);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates[0].Score.ShouldBe(20);
        result.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.MetadataMatched]);
    }

    [Fact]
    public void Map_ArchivedCandidate_ExcludedByDefault_ButIncludedWhenRequested()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a"),
            [Project("project-a", "Project Alpha", ProjectLifecycle.Archived)],
            DefaultNow);

        ProjectResolution excludedByDefault = new ProjectResolutionEngine().Resolve(Context(includeArchived: false), evidence);
        excludedByDefault.Result.ShouldBe(ResolutionResult.NoMatch);
        excludedByDefault.Excluded.Count.ShouldBe(1);
        excludedByDefault.Excluded[0].ReferenceState.ShouldBe(ReferenceState.Archived);
        excludedByDefault.Excluded[0].Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ProjectArchived);

        ProjectResolution includedWhenRequested = new ProjectResolutionEngine().Resolve(Context(includeArchived: true), evidence);
        includedWhenRequested.Result.ShouldBe(ResolutionResult.SingleCandidate);
        includedWhenRequested.Candidates[0].ProjectId.ShouldBe("project-a");
    }

    [Theory]
    [InlineData(ReferenceState.Stale, ProjectContextInclusionDiagnostic.ReferenceStale)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionDiagnostic.ReferenceUnavailable)]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionDiagnostic.ReferenceUnauthorized)]
    public void Map_DegradedConversationTrust_SignalBecomesExclusionNotMatch(
        ReferenceState degraded,
        string expectedDiagnostic)
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a", referenceState: degraded),
            [Project("project-a", "Project Alpha")],
            DefaultNow);

        evidence.Count.ShouldBe(1);
        evidence[0].Signals[0].ReferenceState.ShouldBe(degraded);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), evidence);
        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.Count.ShouldBe(1);
        result.Excluded[0].ProjectId.ShouldBe("project-a");
        result.Excluded[0].ReasonCode.ShouldBe(ProjectReasonCode.ConversationLinked);
        result.Excluded[0].Diagnostic.ShouldBe(expectedDiagnostic);
    }

    [Fact]
    public void Map_CandidateWithoutSignal_IsOmittedNotExcluded()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a"),
            [Project("project-a", "Project Alpha"), Project("project-z", "Zeta")],
            DefaultNow);

        evidence.Count.ShouldBe(1);
        evidence[0].ProjectId.ShouldBe("project-a");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-other")]
    public void Map_ThenEngine_TenantFailClosed_ReturnsNoMatchWithStructuredWarning(string? requestedTenant)
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a"),
            [Project("project-a", "Project Alpha")],
            DefaultNow);

        RecordingLogger<ProjectResolutionEngine> logger = new();

        // Blank authoritative tenant, or a requested tenant that mismatches the authoritative one,
        // both fail closed: every candidate is excluded TenantMismatch and the outcome is NoMatch.
        ProjectResolutionContext context = requestedTenant is null
            ? Context(authoritativeTenantId: null, requestedTenantId: null)
            : Context(authoritativeTenantId: DefaultTenant, requestedTenantId: requestedTenant);

        ProjectResolution result = new ProjectResolutionEngine(logger).Resolve(context, evidence);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.ShouldContain(e => e.ReferenceState == ReferenceState.TenantMismatch);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void Map_ThenEngine_ResultLeaksNoForbiddenContentOrTenant()
    {
        IReadOnlyList<ProjectResolutionCandidateEvidence> evidence = ConversationResolutionEvidenceMapper.Map(
            Conversation(linkedProjectId: "project-a", safeLabel: "Project Alpha"),
            [Project("project-a", "Project Alpha")],
            DefaultNow);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(authoritativeTenantId: DefaultTenant, requestedTenantId: DefaultTenant),
            evidence);

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(result));

        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("tenantId");
        serialized.ShouldNotContain(DefaultTenant);
    }
}
