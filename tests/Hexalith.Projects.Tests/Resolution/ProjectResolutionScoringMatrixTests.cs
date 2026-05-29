// <copyright file="ProjectResolutionScoringMatrixTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Doc-to-code completeness tests for <c>docs/resolution-scoring-heuristic.md</c>.</summary>
public sealed class ProjectResolutionScoringMatrixTests
{
    public static IEnumerable<object[]> ReasonWeightCells()
    {
        yield return new object[] { ProjectReasonCode.ConversationLinked, 50 };
        yield return new object[] { ProjectReasonCode.ProjectFolderMatched, 45 };
        yield return new object[] { ProjectReasonCode.FileReferenceMatched, 35 };
        yield return new object[] { ProjectReasonCode.MemoryMatched, 30 };
        yield return new object[] { ProjectReasonCode.MetadataMatched, 20 };
    }

    [Theory]
    [MemberData(nameof(ReasonWeightCells))]
    public void ReasonCodeWeightCell_CodeAgreesWithDocument(ProjectReasonCode reasonCode, int expectedWeight)
    {
        ProjectResolutionScoringRules.WeightFor(reasonCode).ShouldBe(expectedWeight);
        HeuristicDocument().ShouldContain($"| `{reasonCode}` | {expectedWeight} |");
    }

    [Theory]
    [InlineData(ReferenceState.Unauthorized, ProjectContextInclusionDiagnostic.ReferenceUnauthorized)]
    [InlineData(ReferenceState.Unavailable, ProjectContextInclusionDiagnostic.ReferenceUnavailable)]
    [InlineData(ReferenceState.Stale, ProjectContextInclusionDiagnostic.ReferenceStale)]
    [InlineData(ReferenceState.Archived, ProjectContextInclusionDiagnostic.ReferenceArchived)]
    [InlineData(ReferenceState.TenantMismatch, ProjectContextInclusionDiagnostic.TenantMismatch)]
    [InlineData(ReferenceState.Conflict, ProjectContextInclusionDiagnostic.ReferenceConflict)]
    [InlineData(ReferenceState.InvalidReference, ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier)]
    [InlineData(ReferenceState.Pending, ProjectContextInclusionDiagnostic.ProjectFolderPending)]
    [InlineData(ReferenceState.Excluded, ProjectContextInclusionDiagnostic.ReferenceRedacted)]
    [InlineData(ReferenceState.Ambiguous, ProjectContextInclusionDiagnostic.ReferenceAmbiguous)]
    public void NonIncludedReferenceStateCell_IsExcludedAndNeverScores(ReferenceState state, string expectedDiagnostic)
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [Candidate(signals: [Signal(ProjectReasonCode.ConversationLinked, state)])]);

        result.Result.ShouldBe(ResolutionResult.NoMatch);
        result.Candidates.ShouldBeEmpty();
        result.Excluded.Single().ReferenceState.ShouldBe(state);
        result.Excluded.Single().Diagnostic.ShouldBe(expectedDiagnostic);
    }

    [Theory]
    [InlineData(ProjectReasonCode.MetadataMatched, 20)]
    [InlineData(ProjectReasonCode.MemoryMatched, 30)]
    [InlineData(ProjectReasonCode.FileReferenceMatched, 35)]
    [InlineData(ProjectReasonCode.ConversationLinked, 50)]
    public void ConfidenceBandCells_QualifyWhenSoleCandidate(ProjectReasonCode reasonCode, int expectedScore)
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [Candidate(signals: [Signal(reasonCode, referenceId: reasonCode.ToString(), kind: "metadata")])]);

        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates.Single().Score.ShouldBe(expectedScore);
    }

    [Fact]
    public void DuplicateReasonCodes_CountOnce()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [
                Candidate(signals:
                [
                    Signal(ProjectReasonCode.FileReferenceMatched, referenceId: "file-a", kind: "file"),
                    Signal(ProjectReasonCode.FileReferenceMatched, referenceId: "file-b", kind: "file"),
                ]),
            ]);

        result.Candidates.Single().Score.ShouldBe(35);
        result.Candidates.Single().ReasonCodes.ShouldBe([ProjectReasonCode.FileReferenceMatched]);
    }

    [Fact]
    public void OutcomeThresholdCells_CodeAgreesWithDocument()
    {
        string doc = HeuristicDocument();

        doc.ShouldContain("| 0 | `NoMatch` |");
        doc.ShouldContain("| 1 | `SingleCandidate` |");
        doc.ShouldContain("| 2+ | `MultipleCandidates` |");
    }

    private static string HeuristicDocument()
    {
        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "docs", "resolution-scoring-heuristic.md");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate docs/resolution-scoring-heuristic.md from the test working directory.");
    }
}
