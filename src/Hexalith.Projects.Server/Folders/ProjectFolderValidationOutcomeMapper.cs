// <copyright file="ProjectFolderValidationOutcomeMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure outcome-to-<see cref="ReferenceState"/> mapper for Story 3.4 Refresh. Translates a Folders
/// folder-ACL recheck outcome into the per-reference state the Story 3.1 inclusion policy consumes.
/// </summary>
/// <remarks>
/// <para>
/// Preserves the projection-stored <see cref="ProjectFolderReference.ObservedAt"/> when the recheck
/// confirms the existing state; replaces with the supplied <c>now</c> when the state changes — per
/// the Story 3.1 <c>ObservedAt</c> semantic ("the instant at which this reference state was observed").
/// </para>
/// <para>
/// Preserves the projection-stored <see cref="ReferenceState.Pending"/> on
/// <see cref="ProjectFolderValidationOutcome.Unavailable"/> so the inclusion policy continues to emit
/// the <c>projectFolderPending</c> diagnostic (the Story 2.4 degraded path) rather than
/// <c>referenceUnavailable</c>.
/// </para>
/// <para>
/// Tier-1 purity: no infrastructure, no wall-clock, no sibling-namespace imports.
/// </para>
/// </remarks>
public static class ProjectFolderValidationOutcomeMapper
{
    /// <summary>Maps a Folder ACL outcome onto a refreshed (<see cref="ReferenceState"/>, <see cref="DateTimeOffset"/>) tuple.</summary>
    /// <param name="outcome">The recheck outcome from <see cref="IProjectFolderDirectory.RefreshFolderReferenceAsync"/>.</param>
    /// <param name="projectionStored">The projection-stored reference (fallback for <c>ObservedAt</c> + the <c>Pending</c> preservation rule).</param>
    /// <param name="now">The recheck wall-clock from <c>TimeProvider</c>.</param>
    /// <returns>The mapped <see cref="ReferenceState"/> and the <see cref="DateTimeOffset"/> to stamp on the rechecked reference.</returns>
    public static (ReferenceState State, DateTimeOffset ObservedAt) Map(
        ProjectFolderValidationOutcome outcome,
        ProjectFolderReference projectionStored,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(projectionStored);

        ReferenceState mapped = outcome switch
        {
            ProjectFolderValidationOutcome.Accepted => ReferenceState.Included,
            ProjectFolderValidationOutcome.Archived => ReferenceState.Archived,
            ProjectFolderValidationOutcome.Stale => ReferenceState.Stale,
            ProjectFolderValidationOutcome.Denied => ReferenceState.Unauthorized,
            ProjectFolderValidationOutcome.Unavailable
                when projectionStored.ReferenceState == ReferenceState.Pending
                => ReferenceState.Pending,
            ProjectFolderValidationOutcome.Unavailable => ReferenceState.Unavailable,
            ProjectFolderValidationOutcome.ValidationFailed => ReferenceState.InvalidReference,
            _ => ReferenceState.Unavailable,
        };

        DateTimeOffset observedAt = mapped == projectionStored.ReferenceState
            ? projectionStored.ObservedAt
            : now;

        return (mapped, observedAt);
    }
}
