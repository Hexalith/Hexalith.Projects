// <copyright file="ProjectResolutionTraceMappingTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System;
using System.IO;
using System.Linq;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>
/// AC11 coverage: the five Resolution Trace states (UX-DR9/UX-DR15, Story 5.6) reconstruct from the
/// engine's per-candidate evidence without any persisted trace. The code enum has only
/// <c>NoMatch</c> / <c>SingleCandidate</c> / <c>MultipleCandidates</c>; <c>Resolved</c> maps to
/// <c>SingleCandidate</c>, while <c>Excluded</c> and <c>FailedClosed</c> are per-candidate exclusion rows.
/// </summary>
public sealed class ProjectResolutionTraceMappingTests
{
    [Fact]
    public void Resolve_SingleCandidate_MapsToResolvedTraceState()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), [Candidate()]);

        // Trace state: Resolved.
        result.Result.ShouldBe(ResolutionResult.SingleCandidate);
        result.Candidates.Single().Rank.ShouldBe(1);
    }

    [Fact]
    public void Resolve_NoQualifyingCandidate_MapsToNoMatchTraceState()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), []);

        // Trace state: NoMatch.
        result.Result.ShouldBe(ResolutionResult.NoMatch);
    }

    [Fact]
    public void Resolve_RichScenario_ReconstructsMultipleCandidatesExcludedAndFailedClosedTraceStates()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [
                Candidate("project-a", signals: [Signal(ProjectReasonCode.ConversationLinked)]),
                Candidate("project-b", signals: [Signal(ProjectReasonCode.ConversationLinked, referenceId: "conv-b")]),
                Candidate(
                    "project-archived",
                    lifecycle: ProjectLifecycle.Archived,
                    signals: [Signal(ProjectReasonCode.ConversationLinked, referenceId: "conv-arch")]),
                Candidate(
                    "project-unauthorized",
                    signals: [Signal(ProjectReasonCode.MemoryMatched, ReferenceState.Unauthorized, "memory-unauth", "memory")]),
            ]);

        // Top-level trace state: MultipleCandidates (two qualifying candidates, never silently collapsed).
        result.Result.ShouldBe(ResolutionResult.MultipleCandidates);
        result.Candidates.Select(static c => c.ProjectId).ShouldBe(["project-a", "project-b"]);

        // Trace state: Excluded (archived / policy-excluded candidate, surfaced not silently dropped).
        result.Excluded.ShouldContain(static e => e.ProjectId == "project-archived" && e.ReferenceState == ReferenceState.Archived);

        // Trace state: FailedClosed (unverifiable authorization evidence, surfaced as exclusion).
        result.Excluded.ShouldContain(static e => e.ProjectId == "project-unauthorized" && e.ReferenceState == ReferenceState.Unauthorized);
    }

    [Fact]
    public void HeuristicDocument_DeclaresAllFiveTraceStates()
    {
        string doc = HeuristicDocument();

        doc.ShouldContain("| `Resolved` |");
        doc.ShouldContain("| `NoMatch` |");
        doc.ShouldContain("| `MultipleCandidates` |");
        doc.ShouldContain("| `Excluded` |");
        doc.ShouldContain("| `FailedClosed` |");
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
