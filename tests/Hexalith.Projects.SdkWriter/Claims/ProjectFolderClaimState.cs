// <copyright file="ProjectFolderClaimState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;
using System.Collections.Generic;

/// <summary>
/// Event-sourced state for the AD-35 exclusive Folder claim aggregate
/// (<c>Tenant/project-folder-claims/FolderId</c>).
/// </summary>
public sealed class ProjectFolderClaimState
{
    private readonly Dictionary<string, ProjectFolderClaimReceipt> _acquisitionsByTaskStep = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ProjectFolderClaimReleaseRecord> _releasedTaskSteps = new(StringComparer.Ordinal);

    /// <summary>Gets the owning tenant identifier once initialized.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Gets the owning Folder identifier once initialized.</summary>
    public string? FolderId { get; private set; }

    /// <summary>Gets the stream version (exact generation counter across all claim events).</summary>
    public long Version { get; private set; }

    /// <summary>Gets the currently active claim holder, or null when free or closed.</summary>
    public ProjectFolderClaimReceipt? ActiveClaim { get; private set; }

    /// <summary>Gets a value indicating whether an active claim currently holds this Folder.</summary>
    public bool IsActive => ActiveClaim is not null && !IsClosed;

    /// <summary>Gets a value indicating whether this Folder claim key is permanently closed/quarantined.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>Gets the Safe Metadata close reason when <see cref="IsClosed"/> is <see langword="true"/>.</summary>
    public string? ClosedReasonCode { get; private set; }

    /// <summary>Gets the recorded releases keyed by composite <c>(TaskId, TaskStepKey)</c>.</summary>
    public IReadOnlyDictionary<string, ProjectFolderClaimReleaseRecord> ReleasedTaskSteps => _releasedTaskSteps;

    /// <summary>Looks up a previously acquired claim receipt by exact task and task-step key.</summary>
    /// <param name="taskId">The Durable Task identifier.</param>
    /// <param name="taskStepKey">The deterministic task-step key.</param>
    /// <param name="receipt">The matched receipt, if any.</param>
    /// <returns><see langword="true"/> when a receipt exists for that task and step; otherwise <see langword="false"/>.</returns>
    public bool TryLookupByTaskStep(string taskId, string taskStepKey, out ProjectFolderClaimReceipt? receipt)
    {
        if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(taskStepKey))
        {
            receipt = null;
            return false;
        }

        return _acquisitionsByTaskStep.TryGetValue(TaskStepCompositeKey(taskId, taskStepKey), out receipt);
    }

    /// <summary>Looks up a previously recorded release by exact task and task-step key.</summary>
    /// <param name="taskId">The Durable Task identifier.</param>
    /// <param name="taskStepKey">The deterministic release task-step key.</param>
    /// <param name="record">The recorded release tuple, if any.</param>
    /// <returns><see langword="true"/> when a release record exists; otherwise <see langword="false"/>.</returns>
    public bool TryLookupRelease(string taskId, string taskStepKey, out ProjectFolderClaimReleaseRecord? record)
    {
        if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(taskStepKey))
        {
            record = null;
            return false;
        }

        return _releasedTaskSteps.TryGetValue(TaskStepCompositeKey(taskId, taskStepKey), out record);
    }

    /// <summary>Applies a <see cref="ProjectFolderClaimAcquired"/> event.</summary>
    /// <param name="acquired">The acquisition event.</param>
    public void Apply(ProjectFolderClaimAcquired acquired)
    {
        ArgumentNullException.ThrowIfNull(acquired);
        EnsureIdentity(acquired.TenantId, acquired.FolderId);
        if (IsClosed || IsActive || acquired.Generation != Version + 1)
        {
            throw new InvalidOperationException("Invalid ProjectFolderClaimAcquired state transition in ProjectFolderClaimState.Apply.");
        }

        ProjectFolderClaimReceipt receipt = new(
            acquired.TenantId,
            acquired.FolderId,
            acquired.ProjectId,
            acquired.Generation,
            acquired.TaskId,
            acquired.TaskStepKey,
            acquired.ReceiptId,
            acquired.OccurredAt);

        ActiveClaim = receipt;
        _acquisitionsByTaskStep[TaskStepCompositeKey(acquired.TaskId, acquired.TaskStepKey)] = receipt;
        Version++;
    }

    /// <summary>Applies a <see cref="ProjectFolderClaimReleased"/> event.</summary>
    /// <param name="released">The release event.</param>
    public void Apply(ProjectFolderClaimReleased released)
    {
        ArgumentNullException.ThrowIfNull(released);
        EnsureIdentity(released.TenantId, released.FolderId);

        if (ActiveClaim is { } current
            && string.Equals(current.ProjectId, released.ProjectId, StringComparison.Ordinal)
            && current.Generation == released.ReleasedGeneration)
        {
            ActiveClaim = null;
        }

        _releasedTaskSteps[TaskStepCompositeKey(released.TaskId, released.TaskStepKey)] =
            new ProjectFolderClaimReleaseRecord(released.ProjectId, released.ReleasedGeneration);
        Version++;
    }

    /// <summary>Applies a <see cref="ProjectFolderClaimClosed"/> event.</summary>
    /// <param name="closed">The close event.</param>
    public void Apply(ProjectFolderClaimClosed closed)
    {
        ArgumentNullException.ThrowIfNull(closed);
        EnsureIdentity(closed.TenantId, closed.FolderId);

        ActiveClaim = null;
        IsClosed = true;
        ClosedReasonCode = closed.ReasonCode;
        Version++;
    }

    /// <summary>Applies a persisted <see cref="ProjectFolderClaimRejected"/> event (advances version only).</summary>
    /// <param name="rejected">The rejection event.</param>
    public void Apply(ProjectFolderClaimRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        Version++;
    }

    private void EnsureIdentity(string tenantId, string folderId)
    {
        if (TenantId is null && FolderId is null)
        {
            TenantId = tenantId;
            FolderId = folderId;
            return;
        }

        if (!string.Equals(TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(FolderId, folderId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Foreign claim event in ProjectFolderClaimState.Apply.");
        }
    }

    private static string TaskStepCompositeKey(string taskId, string taskStepKey)
        => $"{taskId.Length}:{taskId}:{taskStepKey.Length}:{taskStepKey}";
}
