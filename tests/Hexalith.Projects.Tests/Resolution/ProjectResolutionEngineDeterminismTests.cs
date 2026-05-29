// <copyright file="ProjectResolutionEngineDeterminismTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Determinism coverage: stable ordering, tiebreaks, and no wall-clock dependency.</summary>
public sealed class ProjectResolutionEngineDeterminismTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Resolve_TieBreaksByProjectIdOrdinal()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [Candidate("project-b"), Candidate("project-a")]);

        result.Result.ShouldBe(ResolutionResult.MultipleCandidates);
        result.Candidates.Select(static c => c.ProjectId).ShouldBe(["project-a", "project-b"]);
        result.Candidates.Select(static c => c.Rank).ShouldBe([1, 2]);
    }

    [Fact]
    public void Resolve_InputReorderingProducesIdenticalOutput()
    {
        ProjectResolutionEngine engine = new();
        ProjectResolutionContext context = Context();
        ProjectResolutionCandidateEvidence a = Candidate("project-a", signals: [Signal(ProjectReasonCode.MetadataMatched, referenceId: "meta-a", kind: "metadata")]);
        ProjectResolutionCandidateEvidence b = Candidate("project-b", signals: [Signal(ProjectReasonCode.MetadataMatched, referenceId: "meta-b", kind: "metadata")]);

        string first = JsonSerializer.Serialize(engine.Resolve(context, [b, a]), Options);
        string second = JsonSerializer.Serialize(engine.Resolve(context, [a, b]), Options);

        second.ShouldBe(first);
    }

    [Fact]
    public void Resolve_ObservedAtComesFromInputNow()
    {
        DateTimeOffset fixedNow = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(now: fixedNow),
            [Candidate()]);

        result.ObservedAt.ShouldBe(fixedNow);
    }
}
