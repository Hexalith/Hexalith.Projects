// <copyright file="ProjectAggregate.Memories.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Optional Memory Reference handlers for the Project aggregate (Story 2.7, FR-10/FR-11). Same purity
/// rules as <see cref="ProjectAggregate"/>: <c>Handle</c> validates, is idempotency-tolerant, fails
/// closed on missing/archived/foreign-tenant projects (emitting rejection reasons, never throwing), and
/// returns events only. Memory references are a bounded optional set; linking/unlinking never clears,
/// replaces, satisfies, or auto-creates the single Project Folder, and never touches file references.
/// The Memories ACL check happens host-side before dispatch — the aggregate records a metadata-only
/// reference only.
/// </summary>
public static partial class ProjectAggregate
{
    /// <summary>
    /// Handles a <see cref="LinkMemory"/> command against the current state.
    /// </summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The link-memory command.</param>
    /// <param name="occurredAt">The event timestamp supplied by the command pipeline.</param>
    /// <returns>An accepted link result, an idempotent replay/no-op, or a rejection.</returns>
    public static ProjectResult Handle(ProjectState state, LinkMemory command, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ProjectResult.Rejected(command, validation.Code, validation.RejectedField);
        }

        if (state.IdempotencyFingerprints.TryGetValue(command.IdempotencyKey, out string? priorFingerprint))
        {
            return string.Equals(priorFingerprint, validation.IdempotencyFingerprint, StringComparison.Ordinal)
                ? ProjectResult.Rejected(command, ProjectResultCode.IdempotentReplay)
                : ProjectResult.Rejected(command, ProjectResultCode.IdempotencyConflict);
        }

        if (!state.IsCreated)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.ProjectNotFound);
        }

        if (!IsSameIdentity(state, command))
        {
            return ProjectResult.Rejected(command, ProjectResultCode.TenantMismatch);
        }

        if (state.Lifecycle == ProjectLifecycle.Archived)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.ProjectIsArchived);
        }

        string memoryReferenceId = command.MemoryReferenceId.Trim();
        string? displayName = string.IsNullOrWhiteSpace(command.MemoryMetadata.DisplayName)
            ? null
            : command.MemoryMetadata.DisplayName.Trim();

        if (state.MemoryReferences.TryGetValue(memoryReferenceId, out ProjectMemoryReference? existing))
        {
            // An identical link (same display metadata) is a logical no-op replay; a link of the same
            // reference id with conflicting safe metadata is a conflict, never a silent overwrite.
            return string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal)
                ? ProjectResult.Rejected(command, ProjectResultCode.IdempotentReplay)
                : ProjectResult.Rejected(command, ProjectResultCode.MemoryReferenceConflict);
        }

        if (state.MemoryReferences.Count >= ProjectState.MaxMemoryReferences)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.MemoryReferenceLimitExceeded);
        }

        MemoryLinked linked = new(
            command.TenantId,
            command.ProjectId.Value,
            memoryReferenceId,
            new ProjectMemoryReferenceMetadata(displayName),
            command.ActorPrincipalId,
            command.CorrelationId,
            command.TaskId,
            command.IdempotencyKey,
            validation.IdempotencyFingerprint!,
            occurredAt);

        return ProjectResult.Accepted(command, ProjectResultCode.MemoryLinked, [linked]);
    }

    /// <summary>
    /// Handles an <see cref="UnlinkMemory"/> command against the current state.
    /// </summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The unlink-memory command.</param>
    /// <param name="occurredAt">The event timestamp supplied by the command pipeline.</param>
    /// <returns>An accepted unlink result, an idempotent no-op for a missing reference, or a rejection.</returns>
    public static ProjectResult Handle(ProjectState state, UnlinkMemory command, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ProjectResult.Rejected(command, validation.Code, validation.RejectedField);
        }

        if (state.IdempotencyFingerprints.TryGetValue(command.IdempotencyKey, out string? priorFingerprint))
        {
            return string.Equals(priorFingerprint, validation.IdempotencyFingerprint, StringComparison.Ordinal)
                ? ProjectResult.Rejected(command, ProjectResultCode.IdempotentReplay)
                : ProjectResult.Rejected(command, ProjectResultCode.IdempotencyConflict);
        }

        if (!state.IsCreated)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.ProjectNotFound);
        }

        if (!IsSameIdentity(state, command))
        {
            return ProjectResult.Rejected(command, ProjectResultCode.TenantMismatch);
        }

        if (state.Lifecycle == ProjectLifecycle.Archived)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.ProjectIsArchived);
        }

        string memoryReferenceId = command.MemoryReferenceId.Trim();

        // Unlinking a reference that is not present is a safe idempotent no-op: the desired end state
        // (no association) already holds, and the underlying Memories case is never touched.
        if (!state.MemoryReferences.ContainsKey(memoryReferenceId))
        {
            return ProjectResult.Rejected(command, ProjectResultCode.IdempotentReplay);
        }

        MemoryUnlinked unlinked = new(
            command.TenantId,
            command.ProjectId.Value,
            memoryReferenceId,
            command.ActorPrincipalId,
            command.CorrelationId,
            command.TaskId,
            command.IdempotencyKey,
            validation.IdempotencyFingerprint!,
            occurredAt);

        return ProjectResult.Accepted(command, ProjectResultCode.MemoryUnlinked, [unlinked]);
    }

    /// <summary>Deterministic-timestamp test overload for link-memory.</summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The link-memory command.</param>
    /// <returns>The handle result with a <see cref="DateTimeOffset.MinValue"/> timestamp.</returns>
    public static ProjectResult Handle(ProjectState state, LinkMemory command)
        => Handle(state, command, DateTimeOffset.MinValue);

    /// <summary>Deterministic-timestamp test overload for unlink-memory.</summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The unlink-memory command.</param>
    /// <returns>The handle result with a <see cref="DateTimeOffset.MinValue"/> timestamp.</returns>
    public static ProjectResult Handle(ProjectState state, UnlinkMemory command)
        => Handle(state, command, DateTimeOffset.MinValue);
}
