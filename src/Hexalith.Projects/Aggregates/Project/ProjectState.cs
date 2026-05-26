// <copyright file="ProjectState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure, replay-built in-memory state for the Project aggregate (AR-3). Mirrors the Folders
/// <c>FolderState</c>: an <see cref="Empty"/> factory, an <see cref="IsCreated"/> flag, the recorded
/// canonical identity, the lifecycle, and an <see cref="IdempotencyFingerprints"/> map used for
/// replay deduplication. State carries no infrastructure dependency.
/// </summary>
/// <param name="IsCreated">Whether a <c>ProjectCreated</c> event has been applied to this stream.</param>
/// <param name="TenantId">The recorded managed tenant identifier, or null before creation.</param>
/// <param name="ProjectId">The recorded project identifier value, or null before creation.</param>
/// <param name="Name">The recorded project name, or null before creation.</param>
/// <param name="Description">The recorded safe description, or null.</param>
/// <param name="SetupMetadata">The recorded safe setup-metadata reference, or null.</param>
/// <param name="Setup">The latest typed metadata-only setup, or null before the first setup update.</param>
/// <param name="ProjectFolder">The metadata-only single Project Folder reference or pending creation state.</param>
/// <param name="Lifecycle">The recorded lifecycle state, or null before creation.</param>
/// <param name="IdempotencyFingerprints">The recorded idempotency-key → fingerprint map for replay dedup.</param>
public sealed record ProjectState(
    bool IsCreated,
    string? TenantId,
    string? ProjectId,
    string? Name,
    string? Description,
    string? SetupMetadata,
    ProjectSetup? Setup,
    ProjectFolderReference? ProjectFolder,
    ProjectLifecycle? Lifecycle,
    IReadOnlyDictionary<string, string> IdempotencyFingerprints)
{
    /// <summary>Gets the empty starting state for a project stream that has no events applied.</summary>
    public static ProjectState Empty { get; } = new(
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        FrozenDictionary<string, string>.Empty);

    /// <summary>
    /// Applies a sequence of project events to this state, enforcing the expected canonical identity on
    /// every event (a misrouted/foreign-tenant event throws).
    /// </summary>
    /// <param name="events">The events to apply in order.</param>
    /// <param name="expectedIdentity">The authoritative canonical identity loaded by the caller.</param>
    /// <returns>The resulting state after applying every event.</returns>
    public ProjectState Apply(IEnumerable<IProjectEvent> events, ProjectIdentity expectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(expectedIdentity);

        ProjectState state = this;
        foreach (IProjectEvent projectEvent in events)
        {
            state = ProjectStateApply.Apply(state, projectEvent, expectedIdentity);
        }

        return state;
    }
}
