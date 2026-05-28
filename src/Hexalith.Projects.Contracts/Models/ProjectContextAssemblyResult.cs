// <copyright file="ProjectContextAssemblyResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// The output of <c>ProjectContextInclusionPolicy.Assemble(...)</c> (AR-9, Story 3.1): the assembled
/// <see cref="ProjectContext"/> plus the deterministic per-candidate <see cref="Evaluations"/> trace
/// Story 3.3 ExplainContextSelection consumes.
/// </summary>
/// <param name="Context">The assembled Project Context (metadata-only).</param>
/// <param name="Evaluations">Per-candidate evaluation trace rows, ordered by (kind, id).</param>
public sealed record ProjectContextAssemblyResult(
    ProjectContext Context,
    IReadOnlyList<ProjectContextEvaluation> Evaluations)
{
    /// <summary>Gets the assembled Project Context.</summary>
    public ProjectContext Context { get; } = Context ?? throw new ArgumentNullException(nameof(Context));

    /// <summary>Gets the per-candidate evaluation trace rows.</summary>
    public IReadOnlyList<ProjectContextEvaluation> Evaluations { get; } = Evaluations ?? throw new ArgumentNullException(nameof(Evaluations));
}
