// <copyright file="ProjectContextAdmission.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>AD-32 admission outcome derived from the pure inclusion allowlist.</summary>
/// <param name="IsSafeDenial">Whether the caller-observable result must be canonical safe denial.</param>
/// <param name="ProjectId">The opaque Project identifier.</param>
/// <param name="Lifecycle">The owning Project lifecycle.</param>
/// <param name="Setup">Metadata-only setup, or null when unavailable or denied.</param>
/// <param name="ProjectFolder">The included Project Folder, or null.</param>
/// <param name="Conversations">Included conversation references.</param>
/// <param name="FileReferences">Included file references.</param>
/// <param name="MemoryReferences">Included memory references.</param>
/// <param name="Excluded">Explicit optional omissions.</param>
/// <param name="Evaluations">Per-candidate evaluations at the same evidence cutoff.</param>
/// <param name="Snapshot">The shared AD-32 snapshot.</param>
public sealed record ProjectContextAdmission(
    bool IsSafeDenial,
    string ProjectId,
    ProjectLifecycle Lifecycle,
    ProjectSetup? Setup,
    ProjectContextReference? ProjectFolder,
    IReadOnlyList<ProjectContextReference> Conversations,
    IReadOnlyList<ProjectContextReference> FileReferences,
    IReadOnlyList<ProjectContextReference> MemoryReferences,
    IReadOnlyList<ProjectContextExclusion> Excluded,
    IReadOnlyList<ProjectContextEvaluation> Evaluations,
    AdmissionSnapshot Snapshot)
{
    /// <summary>Builds the canonical safe-denial admission that discloses no protected context.</summary>
    /// <param name="projectId">The requested project identifier echoed only for internal routing.</param>
    /// <returns>A safe-denial admission.</returns>
    public static ProjectContextAdmission SafeDenial(string projectId)
        => new(
            true,
            projectId,
            ProjectLifecycle.Active,
            Setup: null,
            ProjectFolder: null,
            Conversations: [],
            FileReferences: [],
            MemoryReferences: [],
            Excluded: [],
            Evaluations: [],
            new AdmissionSnapshot(
                AdmissionResponseState.Denied,
                default,
                0,
                [],
                []));

    /// <summary>Projects this admission onto the supported Get response.</summary>
    /// <returns>The metadata-only read response.</returns>
    public ProjectContextReadResponse ToReadResponse()
        => new(
            ProjectId,
            Lifecycle,
            Setup,
            ProjectFolder,
            Conversations,
            FileReferences,
            MemoryReferences,
            Excluded,
            Snapshot);

    /// <summary>Projects this admission onto the supported Explain response.</summary>
    /// <returns>The request-scoped explanation.</returns>
    public ExplainContextSelectionResponse ToExplanation()
        => new(ToReadResponse(), Evaluations);
}
