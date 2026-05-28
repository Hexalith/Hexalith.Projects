// <copyright file="ProjectFileReferenceValidationOutcomeMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure outcome-to-<see cref="ReferenceState"/> mapper for Story 3.4 Refresh. Translates a Folders
/// file-reference ACL recheck outcome into the per-reference state the Story 3.1 inclusion policy
/// consumes.
/// </summary>
/// <remarks>
/// <para>
/// The mapper hands raw <see cref="ProjectFileReferenceValidationOutcome.TenantMismatch"/> through to
/// the inclusion policy as <see cref="ReferenceState.TenantMismatch"/>; the policy then collapses it
/// to <see cref="ReferenceState.Unauthorized"/> with the closed-vocabulary <c>tenantMismatch</c>
/// diagnostic — preserving the layering (mapper translates, policy decides).
/// </para>
/// <para>
/// Preserves the projection-stored <see cref="ProjectFileReference.ObservedAt"/> when the recheck
/// confirms the existing state; replaces with the supplied <c>now</c> when the state changes.
/// </para>
/// <para>
/// Tier-1 purity: no infrastructure, no wall-clock, no sibling-namespace imports.
/// </para>
/// </remarks>
public static class ProjectFileReferenceValidationOutcomeMapper
{
    /// <summary>Maps a File-reference ACL outcome onto a refreshed (<see cref="ReferenceState"/>, <see cref="DateTimeOffset"/>) tuple.</summary>
    /// <param name="outcome">The recheck outcome from <see cref="IProjectFileReferenceDirectory.RefreshFileReferenceAsync"/>.</param>
    /// <param name="projectionStored">The projection-stored reference (fallback for <c>ObservedAt</c>).</param>
    /// <param name="now">The recheck wall-clock from <c>TimeProvider</c>.</param>
    /// <returns>The mapped <see cref="ReferenceState"/> and the <see cref="DateTimeOffset"/> to stamp on the rechecked reference.</returns>
    public static (ReferenceState State, DateTimeOffset ObservedAt) Map(
        ProjectFileReferenceValidationOutcome outcome,
        ProjectFileReference projectionStored,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(projectionStored);

        ReferenceState mapped = outcome switch
        {
            ProjectFileReferenceValidationOutcome.Accepted => ReferenceState.Included,
            ProjectFileReferenceValidationOutcome.Denied => ReferenceState.Unauthorized,
            ProjectFileReferenceValidationOutcome.Redacted => ReferenceState.Excluded,
            ProjectFileReferenceValidationOutcome.Archived => ReferenceState.Archived,
            ProjectFileReferenceValidationOutcome.Stale => ReferenceState.Stale,
            ProjectFileReferenceValidationOutcome.TenantMismatch => ReferenceState.TenantMismatch,
            ProjectFileReferenceValidationOutcome.Unavailable => ReferenceState.Unavailable,
            ProjectFileReferenceValidationOutcome.ValidationFailed => ReferenceState.InvalidReference,
            _ => ReferenceState.Unavailable,
        };

        DateTimeOffset observedAt = mapped == projectionStored.ReferenceState
            ? projectionStored.ObservedAt
            : now;

        return (mapped, observedAt);
    }
}
