// <copyright file="ProjectContextShadowComparator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Reads;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Compares legacy and supported Get/Explain results on a frozen representable corpus after AD-32
/// normalization. Known legacy deficits are not rewritten into equality.
/// </summary>
public static class ProjectContextShadowComparator
{
    /// <summary>Compares a legacy Get body with a supported Get snapshot.</summary>
    /// <param name="legacy">The legacy assembled Project Context.</param>
    /// <param name="supported">The supported read response.</param>
    /// <returns>Whether the comparable surface matches after AD-32 normalization.</returns>
    public static ProjectContextShadowComparison CompareGet(ProjectContext legacy, ProjectContextReadResponse supported)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(supported);

        if (legacy.AssemblyOutcome is ProjectContextAssemblyOutcome.Unauthorized
            or ProjectContextAssemblyOutcome.ProjectUnavailable)
        {
            return ProjectContextShadowComparison.Diverged("legacy-safe-denial");
        }

        AdmissionResponseState expected = NormalizeLegacyState(legacy);
        if (expected != supported.Snapshot.ResponseState)
        {
            return ProjectContextShadowComparison.Diverged("response-state");
        }

        if (!string.Equals(legacy.ProjectId, supported.ProjectId, StringComparison.Ordinal)
            || legacy.Lifecycle != supported.Lifecycle)
        {
            return ProjectContextShadowComparison.Diverged("identity");
        }

        if (!SameReference(legacy.ProjectFolder, supported.ProjectFolder)
            || !SameReferences(legacy.Conversations, supported.Conversations)
            || !SameReferences(legacy.FileReferences, supported.FileReferences)
            || !SameReferences(legacy.MemoryReferences, supported.MemoryReferences)
            || !SameExclusions(legacy.Excluded, supported.Excluded))
        {
            return ProjectContextShadowComparison.Diverged("references");
        }

        return ProjectContextShadowComparison.Match;
    }

    /// <summary>Compares a legacy Explain body with a supported Explain snapshot.</summary>
    /// <param name="legacy">The legacy explanation.</param>
    /// <param name="supported">The supported explanation.</param>
    /// <returns>Whether the comparable surface matches after AD-32 normalization.</returns>
    public static ProjectContextShadowComparison CompareExplain(
        ProjectContextExplanation legacy,
        ExplainContextSelectionResponse supported)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(supported);

        ProjectContextShadowComparison context = CompareGet(legacy.Context, supported.Context);
        if (!context.Equivalent)
        {
            return context;
        }

        if (!SameEvaluations(legacy.Evaluations, supported.Evaluations))
        {
            return ProjectContextShadowComparison.Diverged("evaluations");
        }

        return ProjectContextShadowComparison.Match;
    }

    /// <summary>Maps a legacy assembled context onto the shared AD-32 response state.</summary>
    /// <param name="legacy">The legacy assembled context.</param>
    /// <returns>The normalized AD-32 state.</returns>
    public static AdmissionResponseState NormalizeLegacyState(ProjectContext legacy)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        if (legacy.AssemblyOutcome is ProjectContextAssemblyOutcome.Unauthorized
            or ProjectContextAssemblyOutcome.ProjectUnavailable)
        {
            return AdmissionResponseState.Denied;
        }

        if (legacy.Freshness == ProjectContextFreshness.Stale
            || legacy.ProjectFolder is not { ReferenceState: ReferenceState.Included })
        {
            return AdmissionResponseState.Unavailable;
        }

        return legacy.Excluded.Count > 0
            ? AdmissionResponseState.Partial
            : AdmissionResponseState.Complete;
    }

    private static bool SameReference(ProjectContextReference? left, ProjectContextReference? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return string.Equals(left.ReferenceKind, right.ReferenceKind, StringComparison.Ordinal)
            && string.Equals(left.ReferenceId, right.ReferenceId, StringComparison.Ordinal)
            && left.ReferenceState == right.ReferenceState;
    }

    private static bool SameReferences(
        IReadOnlyList<ProjectContextReference> left,
        IReadOnlyList<ProjectContextReference> right)
        => left.Count == right.Count
            && left.Zip(right, SameReference).All(static equal => equal);

    private static bool SameExclusions(
        IReadOnlyList<ProjectContextExclusion> left,
        IReadOnlyList<ProjectContextExclusion> right)
        => left.Count == right.Count
            && left.Zip(right, static (legacy, supported) =>
                    string.Equals(legacy.ReferenceKind, supported.ReferenceKind, StringComparison.Ordinal)
                    && string.Equals(legacy.ReferenceId, supported.ReferenceId, StringComparison.Ordinal)
                    && legacy.ReferenceState == supported.ReferenceState
                    && legacy.FailedCheck == supported.FailedCheck)
                .All(static equal => equal);

    private static bool SameEvaluations(
        IReadOnlyList<ProjectContextEvaluation> left,
        IReadOnlyList<ProjectContextEvaluation> right)
        => left.Count == right.Count
            && left.Zip(right, static (legacy, supported) =>
                    string.Equals(legacy.ReferenceKind, supported.ReferenceKind, StringComparison.Ordinal)
                    && string.Equals(legacy.ReferenceId, supported.ReferenceId, StringComparison.Ordinal)
                    && legacy.ResultState == supported.ResultState
                    && legacy.FailedCheck == supported.FailedCheck)
                .All(static equal => equal);
}
