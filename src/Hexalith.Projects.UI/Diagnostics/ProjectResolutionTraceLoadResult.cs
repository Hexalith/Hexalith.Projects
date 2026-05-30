// <copyright file="ProjectResolutionTraceLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe UI result for the resolution trace workbench.
/// </summary>
/// <param name="Trace">The transient trace descriptor, when a query succeeded.</param>
/// <param name="Candidates">Transient candidate comparison rows.</param>
/// <param name="Exclusions">Transient exclusion evidence rows.</param>
/// <param name="Feedback">Safe query feedback, when no trace should render.</param>
public sealed record ProjectResolutionTraceLoadResult(
    ProjectResolutionTraceProjection? Trace,
    IReadOnlyList<ProjectResolutionTraceCandidateProjection> Candidates,
    IReadOnlyList<ProjectResolutionTraceExclusionProjection> Exclusions,
    ProjectConsoleFeedback? Feedback)
{
    /// <summary>Creates a successful trace result.</summary>
    public static ProjectResolutionTraceLoadResult FromTrace(
        ProjectResolutionTraceProjection trace,
        IReadOnlyList<ProjectResolutionTraceCandidateProjection> candidates,
        IReadOnlyList<ProjectResolutionTraceExclusionProjection> exclusions)
        => new(trace, candidates, exclusions, null);

    /// <summary>Creates a feedback-only result.</summary>
    public static ProjectResolutionTraceLoadResult FromFeedback(ProjectConsoleFeedback feedback)
        => new(null, [], [], feedback);
}
