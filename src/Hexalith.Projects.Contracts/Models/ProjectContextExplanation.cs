// <copyright file="ProjectContextExplanation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Wire-body wrapper for Story 3.3 ExplainContextSelection (FR-17, UJ-4, AR-9): carries the assembled
/// <see cref="ProjectContext"/> plus the per-candidate <see cref="ProjectContextEvaluation"/> trace
/// emitted by <c>ProjectContextInclusionPolicy</c> (Story 3.1) so operators can read the include /
/// exclude verdict for every candidate without re-running the assembly.
/// </summary>
/// <remarks>
/// <para>
/// This is the external HTTP contract returned by
/// <c>GET /api/v1/projects/{projectId}/context/explain</c>. The structurally identical
/// <see cref="ProjectContextAssemblyResult"/> remains the policy's INTERNAL result type — both records
/// may evolve additively without forcing wire-compatibility on the other.
/// </para>
/// <para>
/// The wrapper carries no tenant authority on the wire (FS-8 / SM-3): <see cref="Context"/> already
/// suppresses tenant identity via <c>JsonIgnore</c>, and <see cref="Evaluations"/> rows only carry
/// closed-vocabulary diagnostic strings structurally enforced by
/// <see cref="ProjectContextEvaluation"/>'s constructor.
/// </para>
/// </remarks>
/// <param name="Context">The assembled metadata-only Project Context (same shape Story 3.2 surfaces on <c>GET /context</c>).</param>
/// <param name="Evaluations">Per-candidate evaluation trace, ordered by <c>(ReferenceKind, ReferenceId)</c> Ordinal.</param>
public sealed record ProjectContextExplanation(
    ProjectContext Context,
    IReadOnlyList<ProjectContextEvaluation> Evaluations)
{
    /// <summary>Gets the assembled Project Context.</summary>
    public ProjectContext Context { get; } = Context ?? throw new ArgumentNullException(nameof(Context));

    /// <summary>Gets the per-candidate evaluation trace; closed-vocabulary diagnostics structurally enforced by <see cref="ProjectContextEvaluation"/>.</summary>
    public IReadOnlyList<ProjectContextEvaluation> Evaluations { get; } = Evaluations ?? throw new ArgumentNullException(nameof(Evaluations));

    /// <summary>
    /// Composition convenience — returns a wrapper around <see cref="ProjectContext.Unauthorized"/>
    /// with empty <see cref="Evaluations"/>. Never reached on the wire: safe-denial collapses to HTTP
    /// 404 Problem Details, not a <see cref="ProjectContextExplanation"/> body.
    /// </summary>
    /// <param name="requestedTenantId">The requested tenant identifier (echoed metadata-only).</param>
    /// <param name="projectId">The requested project identifier (echoed metadata-only).</param>
    /// <param name="now">The assembly observation instant.</param>
    /// <param name="freshness">The freshness signal at the time of the unauthorized verdict.</param>
    /// <returns>An empty wrapper around the canonical unauthorized Project Context.</returns>
    public static ProjectContextExplanation Empty(
        string requestedTenantId,
        string projectId,
        DateTimeOffset now,
        ProjectContextFreshness freshness)
        => new(
            ProjectContext.Unauthorized(requestedTenantId, projectId, now, freshness),
            Array.Empty<ProjectContextEvaluation>());
}
