// <copyright file="ProjectResolutionContractValidationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System;
using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Constructor validation, null guards, and name-based JSON coverage for resolution contracts.</summary>
public sealed class ProjectResolutionContractValidationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CandidateEvidence_NullOrWhitespaceProjectId_Throws(string? projectId)
        => Should.Throw<ArgumentException>(() =>
            new ProjectResolutionCandidateEvidence(projectId!, "Project", ProjectLifecycle.Active, []));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CandidateEvidence_WhitespaceDisplayName_NormalizesToNull(string displayName)
    {
        ProjectResolutionCandidateEvidence candidate = new("project-a", displayName, ProjectLifecycle.Active, []);

        candidate.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void CandidateEvidence_NullSignals_NormalizesToEmpty()
    {
        ProjectResolutionCandidateEvidence candidate = new("project-a", null, ProjectLifecycle.Active, null!);

        candidate.Signals.ShouldNotBeNull();
        candidate.Signals.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchSignal_NullOrWhitespaceReferenceId_Throws(string? referenceId)
        => Should.Throw<ArgumentException>(() =>
            new ProjectResolutionMatchSignal("conversation", referenceId!, ProjectReasonCode.ConversationLinked, ReferenceState.Included, DefaultNow));

    [Fact]
    public void ResolutionCandidate_EmptyReasonCodes_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ResolutionCandidate("project-a", null, [], Rank: 1, Score: 0));

    [Fact]
    public void ResolutionCandidate_InvalidRank_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() =>
            new ResolutionCandidate("project-a", null, [ProjectReasonCode.MetadataMatched], Rank: 0, Score: 20));

    [Fact]
    public void ResolutionCandidate_NegativeScore_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() =>
            new ResolutionCandidate("project-a", null, [ProjectReasonCode.MetadataMatched], Rank: 1, Score: -1));

    [Fact]
    public void ResolutionCandidate_DuplicateReasonCodes_AreDeduplicated()
    {
        ResolutionCandidate candidate = new(
            "project-a",
            null,
            [ProjectReasonCode.MetadataMatched, ProjectReasonCode.MetadataMatched],
            Rank: 1,
            Score: 20);

        candidate.ReasonCodes.ShouldBe([ProjectReasonCode.MetadataMatched]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchSignal_NullOrWhitespaceReferenceKind_Throws(string? referenceKind)
        => Should.Throw<ArgumentException>(() =>
            new ProjectResolutionMatchSignal(referenceKind!, "conversation-001", ProjectReasonCode.ConversationLinked, ReferenceState.Included, DefaultNow));

    [Fact]
    public void ResolutionExclusion_OutOfVocabularyDiagnostic_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ResolutionExclusion("project-a", null, ReferenceState.Unauthorized, null, "raw upstream message"));

    [Fact]
    public void Resolve_NullContext_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectResolutionEngine().Resolve(context: null!, [Candidate()]));

    [Fact]
    public void Resolve_NullCandidates_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectResolutionEngine().Resolve(Context(), candidates: null!));

    [Fact]
    public void ProjectResolution_JsonRoundTrip_UsesNameBasedEnums()
    {
        ProjectResolution original = new ProjectResolutionEngine().Resolve(Context(), [Candidate()]);

        string serialized = JsonSerializer.Serialize(original, Options);
        ProjectResolution? roundTripped = JsonSerializer.Deserialize<ProjectResolution>(serialized, Options);

        serialized.ShouldContain("\"result\":\"SingleCandidate\"");
        roundTripped.ShouldNotBeNull();
        roundTripped.Result.ShouldBe(ResolutionResult.SingleCandidate);
        roundTripped.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.ConversationLinked]);
    }

    [Fact]
    public void ProjectResolution_AdditiveDeserializationTolerance_IgnoresUnknownProperties()
    {
        const string Json = """
        {
          "result": "NoMatch",
          "candidates": [],
          "excluded": [],
          "observedAt": "2026-05-28T12:00:00+00:00",
          "futureField": "ignored"
        }
        """;

        ProjectResolution? result = JsonSerializer.Deserialize<ProjectResolution>(Json, Options);

        result.ShouldNotBeNull();
        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
    }
}
