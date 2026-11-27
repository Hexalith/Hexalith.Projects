// <copyright file="ProjectSdkState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Aggregates;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// SDK aggregate state wrapper around pure <see cref="ProjectState"/> (AD-17, Story 7.1).
/// Rehydrates all 14 <see cref="IProjectEvent"/> types and all 8 persisted rejection event types
/// so <see cref="Version"/> matches the authoritative stream head without drift.
/// </summary>
public sealed class ProjectSdkState
{
    private readonly Dictionary<string, string> _appliedTaskSteps = new(StringComparer.Ordinal);

    /// <summary>Gets the pure folded domain state.</summary>
    public ProjectState DomainState { get; private set; } = ProjectState.Empty;

    /// <summary>Gets the stream version (count of all applied events including rejections).</summary>
    public long Version { get; private set; }

    /// <summary>Gets the map of committed task-step keys to idempotency fingerprints.</summary>
    public IReadOnlyDictionary<string, string> AppliedTaskSteps => _appliedTaskSteps;

    /// <summary>Applies a <see cref="ProjectCreated"/> event.</summary>
    /// <param name="created">The event to apply.</param>
    public void Apply(ProjectCreated created)
    {
        ArgumentNullException.ThrowIfNull(created);
        ApplyProjectEvent(created);
        if (!string.IsNullOrWhiteSpace(created.TaskStepKey))
        {
            _appliedTaskSteps[created.TaskStepKey] = created.IdempotencyFingerprint;
        }
    }

    /// <summary>Applies a <see cref="ProjectCreationAbandoned"/> event.</summary>
    /// <param name="abandoned">The event to apply.</param>
    public void Apply(ProjectCreationAbandoned abandoned)
    {
        ArgumentNullException.ThrowIfNull(abandoned);
        ApplyProjectEvent(abandoned);
        if (!string.IsNullOrWhiteSpace(abandoned.TaskStepKey))
        {
            _appliedTaskSteps[abandoned.TaskStepKey] = abandoned.IdempotencyFingerprint;
        }
    }

    /// <summary>Applies a <see cref="ProjectSetupUpdated"/> event.</summary>
    /// <param name="updated">The event to apply.</param>
    public void Apply(ProjectSetupUpdated updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        ApplyProjectEvent(updated);
    }

    /// <summary>Applies a <see cref="ProjectFolderSet"/> event.</summary>
    /// <param name="folderSet">The event to apply.</param>
    public void Apply(ProjectFolderSet folderSet)
    {
        ArgumentNullException.ThrowIfNull(folderSet);
        ApplyProjectEvent(folderSet);
    }

    /// <summary>Applies a historical <see cref="ProjectFolderCreationPending"/> event.</summary>
    /// <param name="pending">The event to apply.</param>
    public void Apply(ProjectFolderCreationPending pending)
    {
        ArgumentNullException.ThrowIfNull(pending);
        ApplyProjectEvent(pending);
    }

    /// <summary>Applies a <see cref="FileReferenceLinked"/> event.</summary>
    /// <param name="linked">The event to apply.</param>
    public void Apply(FileReferenceLinked linked)
    {
        ArgumentNullException.ThrowIfNull(linked);
        ApplyProjectEvent(linked);
    }

    /// <summary>Applies a <see cref="FileReferenceUnlinked"/> event.</summary>
    /// <param name="unlinked">The event to apply.</param>
    public void Apply(FileReferenceUnlinked unlinked)
    {
        ArgumentNullException.ThrowIfNull(unlinked);
        ApplyProjectEvent(unlinked);
    }

    /// <summary>Applies a <see cref="MemoryLinked"/> event.</summary>
    /// <param name="linked">The event to apply.</param>
    public void Apply(MemoryLinked linked)
    {
        ArgumentNullException.ThrowIfNull(linked);
        ApplyProjectEvent(linked);
    }

    /// <summary>Applies a <see cref="MemoryUnlinked"/> event.</summary>
    /// <param name="unlinked">The event to apply.</param>
    public void Apply(MemoryUnlinked unlinked)
    {
        ArgumentNullException.ThrowIfNull(unlinked);
        ApplyProjectEvent(unlinked);
    }

    /// <summary>Applies a <see cref="ProjectArchived"/> event.</summary>
    /// <param name="archived">The event to apply.</param>
    public void Apply(ProjectArchived archived)
    {
        ArgumentNullException.ThrowIfNull(archived);
        ApplyProjectEvent(archived);
    }

    /// <summary>Applies a <see cref="ProjectRestored"/> event.</summary>
    /// <param name="restored">The event to apply.</param>
    public void Apply(ProjectRestored restored)
    {
        ArgumentNullException.ThrowIfNull(restored);
        ApplyProjectEvent(restored);
    }

    /// <summary>Applies a <see cref="ProjectResolutionConfirmed"/> event.</summary>
    /// <param name="confirmed">The event to apply.</param>
    public void Apply(ProjectResolutionConfirmed confirmed)
    {
        ArgumentNullException.ThrowIfNull(confirmed);
        ApplyProjectEvent(confirmed);
    }

    /// <summary>Applies a <see cref="ProjectChatBotAuthorityGranted"/> event.</summary>
    /// <param name="granted">The event to apply.</param>
    public void Apply(ProjectChatBotAuthorityGranted granted)
    {
        ArgumentNullException.ThrowIfNull(granted);
        ApplyProjectEvent(granted);
    }

    /// <summary>Applies a <see cref="ProjectChatBotAuthorityRevoked"/> event.</summary>
    /// <param name="revoked">The event to apply.</param>
    public void Apply(ProjectChatBotAuthorityRevoked revoked)
    {
        ArgumentNullException.ThrowIfNull(revoked);
        ApplyProjectEvent(revoked);
    }

    /// <summary>Applies a persisted <see cref="ProjectCreationRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectCreationRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectArchiveRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectArchiveRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectRestoreRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectRestoreRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectSetupUpdateRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectSetupUpdateRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectReferenceLinkRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectReferenceLinkRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectReferenceUnlinkRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectReferenceUnlinkRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectResolutionConfirmationRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectResolutionConfirmationRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ApplyRejectionEvent(rejected.TenantId, rejected.ProjectId?.Value);
    }

    /// <summary>Applies a persisted <see cref="ProjectChatBotAuthorityRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectChatBotAuthorityRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        Version++;
    }

    private void ApplyRejectionEvent(string? tenantId, string? projectId)
    {
        if (!string.IsNullOrWhiteSpace(DomainState.TenantId)
            && !string.IsNullOrWhiteSpace(DomainState.ProjectId))
        {
            if (!string.Equals(DomainState.TenantId, tenantId, StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(projectId) && !string.Equals(DomainState.ProjectId, projectId, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Foreign rejection event in ProjectSdkState.Apply.");
            }
        }

        Version++;
    }

    private void ApplyProjectEvent(IProjectEvent projectEvent)
    {
        ProjectIdentity expectedIdentity = !string.IsNullOrWhiteSpace(DomainState.TenantId)
            && !string.IsNullOrWhiteSpace(DomainState.ProjectId)
            ? new ProjectIdentity(DomainState.TenantId, new ProjectId(DomainState.ProjectId))
            : new ProjectIdentity(projectEvent.TenantId, new ProjectId(projectEvent.ProjectId));

        DomainState = ProjectStateApply.Apply(DomainState, projectEvent, expectedIdentity);
        Version++;
    }
}
