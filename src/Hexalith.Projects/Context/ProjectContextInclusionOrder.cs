// <copyright file="ProjectContextInclusionOrder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Single source of truth for the AR-9 inclusion-check evaluation order (Story 3.1). Every Epic 3
/// story (3.2 Get, 3.3 Explain, 3.4 Refresh, 3.5 GetConversationStartSetup) consumes this sequence
/// directly. Duplicating the order anywhere else is a forbidden anti-pattern.
/// </summary>
/// <remarks>
/// The order matches <c>AuthorizationOrder.LayeredProjectAuthorization</c> in spirit: outer checks
/// short-circuit the assembly, inner checks emit per-candidate exclusion rows but still let the
/// rest of the context assemble.
/// </remarks>
public static class ProjectContextInclusionOrder
{
    /// <summary>
    /// Gets the ordered evaluation chain for <c>ProjectContextInclusionPolicy</c>. The sequence is:
    /// <see cref="ProjectContextInclusionCheck.TenantAuthority"/> →
    /// <see cref="ProjectContextInclusionCheck.ProjectVisibility"/> →
    /// <see cref="ProjectContextInclusionCheck.ProjectLifecycle"/> →
    /// <see cref="ProjectContextInclusionCheck.ReferenceAuthorization"/> →
    /// <see cref="ProjectContextInclusionCheck.ReferenceLifecycle"/> →
    /// <see cref="ProjectContextInclusionCheck.ReferenceFreshness"/> →
    /// <see cref="ProjectContextInclusionCheck.ReferenceKindAllowlist"/>.
    /// </summary>
    public static IReadOnlyList<ProjectContextInclusionCheck> Sequence { get; } =
    [
        ProjectContextInclusionCheck.TenantAuthority,
        ProjectContextInclusionCheck.ProjectVisibility,
        ProjectContextInclusionCheck.ProjectLifecycle,
        ProjectContextInclusionCheck.ReferenceAuthorization,
        ProjectContextInclusionCheck.ReferenceLifecycle,
        ProjectContextInclusionCheck.ReferenceFreshness,
        ProjectContextInclusionCheck.ReferenceKindAllowlist,
    ];

    /// <summary>The set of allowlisted reference kinds the final check enforces.</summary>
    public static IReadOnlyList<string> AllowlistedReferenceKinds { get; } =
    [
        "folder",
        "file",
        "memory",
        "conversation",
    ];

    /// <summary>
    /// Returns whether the supplied reference kind passes the
    /// <see cref="ProjectContextInclusionCheck.ReferenceKindAllowlist"/> check (Ordinal comparison).
    /// </summary>
    /// <param name="referenceKind">The candidate reference kind.</param>
    /// <returns><see langword="true"/> when allowlisted; otherwise <see langword="false"/>.</returns>
    public static bool IsAllowlisted(string? referenceKind)
    {
        if (string.IsNullOrWhiteSpace(referenceKind))
        {
            return false;
        }

        foreach (string allowed in AllowlistedReferenceKinds)
        {
            if (string.Equals(referenceKind, allowed, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
