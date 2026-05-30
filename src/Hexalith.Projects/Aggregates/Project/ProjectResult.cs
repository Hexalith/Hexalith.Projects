// <copyright file="ProjectResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;
using System.Collections.Generic;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure result of <see cref="ProjectAggregate"/> <c>Handle</c> (AR-3). Mirrors the Folders
/// <c>FolderResult</c>: an accepted result carries the emitted success events; a rejected result
/// carries the control-flow <see cref="ProjectResultCode"/> and safe-echoed identity fields (funneled
/// through <see cref="SafePassthrough"/> so a rejection can never echo unsafe input). A single result
/// is <b>either</b> Accepted-with-<c>ProjectCreated</c> <b>or</b> Rejected-with-a-rejection-reason,
/// never both (FS-4).
/// </summary>
/// <param name="Code">The control-flow result code.</param>
/// <param name="TenantId">The safe-echoed tenant identifier, or null.</param>
/// <param name="ProjectId">The safe-echoed project identifier, or null.</param>
/// <param name="ActorPrincipalId">The safe-echoed actor principal identifier, or null.</param>
/// <param name="CorrelationId">The safe-echoed correlation identifier, or null.</param>
/// <param name="TaskId">The safe-echoed task identifier, or null.</param>
/// <param name="IdempotencyKey">The safe-echoed idempotency key, or null.</param>
/// <param name="CommandType">The safe command type discriminator.</param>
/// <param name="RejectedField">The NAME of the rejected field (never its value), or null.</param>
/// <param name="Events">The emitted success events (empty on rejection).</param>
/// <param name="ReferenceKind">Optional sibling reference kind for reference-link rejections.</param>
/// <param name="ReferenceId">Optional sibling reference identifier for reference-link rejections.</param>
public sealed record ProjectResult(
    ProjectResultCode Code,
    string? TenantId,
    string? ProjectId,
    string? ActorPrincipalId,
    string? CorrelationId,
    string? TaskId,
    string? IdempotencyKey,
    string CommandType,
    string? RejectedField,
    IReadOnlyList<IProjectEvent> Events,
    string? ReferenceKind = null,
    string? ReferenceId = null)
{
    /// <summary>Gets a value indicating whether the command was accepted (a success event was emitted).</summary>
    public bool IsAccepted => Code is ProjectResultCode.Created
        or ProjectResultCode.SetupUpdated
        or ProjectResultCode.Archived
        or ProjectResultCode.FolderSet
        or ProjectResultCode.FileReferenceLinked
        or ProjectResultCode.FileReferenceUnlinked
        or ProjectResultCode.MemoryLinked
        or ProjectResultCode.MemoryUnlinked
        or ProjectResultCode.ProjectResolutionConfirmed;

    /// <summary>
    /// Gets a value indicating whether the result is a logical idempotent replay (no second event;
    /// the prior command's event already landed).
    /// </summary>
    public bool IsIdempotentReplay => Code == ProjectResultCode.IdempotentReplay;

    /// <summary>Creates an accepted result carrying the emitted success events.</summary>
    /// <param name="command">The command that produced the events.</param>
    /// <param name="code">The accepted result code.</param>
    /// <param name="events">The emitted success events.</param>
    /// <returns>An accepted <see cref="ProjectResult"/>.</returns>
    public static ProjectResult Accepted(IProjectCommand command, ProjectResultCode code, IReadOnlyList<IProjectEvent> events)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(events);

        return new(
            code,
            SafePassthrough(command.TenantId),
            SafePassthrough(command.ProjectId?.Value),
            SafePassthrough(command.ActorPrincipalId),
            SafePassthrough(command.CorrelationId),
            SafePassthrough(command.TaskId),
            SafePassthrough(command.IdempotencyKey),
            SafePassthrough(command.CommandType) ?? string.Empty,
            null,
            events);
    }

    /// <summary>Creates an accepted create result carrying the emitted success events.</summary>
    /// <param name="command">The create command.</param>
    /// <param name="events">The emitted success events.</param>
    /// <returns>An accepted <see cref="ProjectResult"/>.</returns>
    public static ProjectResult Accepted(CreateProject command, IReadOnlyList<IProjectEvent> events)
        => Accepted(command, ProjectResultCode.Created, events);

    /// <summary>Creates a rejection result, safe-echoing the command identity fields and the field NAME only.</summary>
    /// <param name="command">The command that was rejected.</param>
    /// <param name="code">The control-flow result code (never <see cref="ProjectResultCode.Created"/>).</param>
    /// <param name="rejectedField">The NAME of the rejected field (never its value), or null.</param>
    /// <returns>A rejected <see cref="ProjectResult"/> carrying no events.</returns>
    public static ProjectResult Rejected(IProjectCommand command, ProjectResultCode code, string? rejectedField = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        (string? referenceKind, string? referenceId) = command switch
        {
            SetProjectFolder setProjectFolder => ("folder", SafeReferenceIdentifier(setProjectFolder.FolderId)),
            LinkFileReference linkFileReference => ("file", SafeReferenceIdentifier(linkFileReference.FileReferenceId)),
            UnlinkFileReference unlinkFileReference => ("file", SafeReferenceIdentifier(unlinkFileReference.FileReferenceId)),
            LinkMemory linkMemory => ("memory", SafeReferenceIdentifier(linkMemory.MemoryReferenceId)),
            UnlinkMemory unlinkMemory => ("memory", SafeReferenceIdentifier(unlinkMemory.MemoryReferenceId)),
            ConfirmProjectResolution confirmProjectResolution => ("conversation", SafeReferenceIdentifier(confirmProjectResolution.ConversationId)),
            _ => (null, null),
        };

        return Rejected(
            command.CommandType,
            command.TenantId,
            command.ProjectId?.Value,
            command.ActorPrincipalId,
            command.CorrelationId,
            command.TaskId,
            command.IdempotencyKey,
            code,
            rejectedField,
            referenceKind,
            referenceId);
    }

    /// <summary>Creates a rejection result from already separated safe identity fields.</summary>
    /// <param name="commandType">The command type discriminator.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="actorPrincipalId">The actor identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="code">The control-flow result code.</param>
    /// <param name="rejectedField">The rejected field name.</param>
    /// <returns>A rejected <see cref="ProjectResult"/> carrying no events.</returns>
    public static ProjectResult Rejected(
        string commandType,
        string? tenantId,
        string? projectId,
        string? actorPrincipalId,
        string? correlationId,
        string? taskId,
        string? idempotencyKey,
        ProjectResultCode code,
        string? rejectedField = null,
        string? referenceKind = null,
        string? referenceId = null)
        => new(
            code,
            SafePassthrough(tenantId),
            SafePassthrough(projectId),
            SafePassthrough(actorPrincipalId),
            SafePassthrough(correlationId),
            SafePassthrough(taskId),
            SafePassthrough(idempotencyKey),
            SafePassthrough(commandType) ?? string.Empty,
            rejectedField,
            [],
            SafePassthrough(referenceKind),
            SafeReferenceIdentifier(referenceId));

    /// <summary>
    /// Maps the aggregate-internal control-flow code to the externally-surfaced shared
    /// <see cref="ReferenceState"/> rejection reason (AR-18). Never mints a parallel error enum.
    /// </summary>
    /// <returns>The canonical shared-vocabulary reason code for a rejection.</returns>
    public ReferenceState ToRejectionReason() => Code switch
    {
        ProjectResultCode.Unauthorized => ReferenceState.Unauthorized,
        ProjectResultCode.TenantMismatch => ReferenceState.TenantMismatch,
        ProjectResultCode.DuplicateProject => ReferenceState.Conflict,
        ProjectResultCode.ProjectAlreadyArchived => ReferenceState.Archived,
        ProjectResultCode.ProjectIsArchived => ReferenceState.Archived,
        ProjectResultCode.ProjectFolderReplacementRequiresConfirmation => ReferenceState.Conflict,
        ProjectResultCode.FileReferenceConflict => ReferenceState.Conflict,
        ProjectResultCode.FileReferenceLimitExceeded => ReferenceState.Conflict,
        ProjectResultCode.MemoryReferenceConflict => ReferenceState.Conflict,
        ProjectResultCode.MemoryReferenceLimitExceeded => ReferenceState.Conflict,
        ProjectResultCode.IdempotencyConflict => ReferenceState.Conflict,
        ProjectResultCode.ProjectNotFound => ReferenceState.InvalidReference,
        ProjectResultCode.ValidationFailed => ReferenceState.InvalidReference,
        ProjectResultCode.StateTransitionInvalid => ReferenceState.InvalidReference,
        // Created / IdempotentReplay are not rejections; if a caller maps them as a reason it is a
        // control-flow bug — surface a conservative InvalidReference rather than implying success.
        _ => ReferenceState.InvalidReference,
    };

    /// <summary>
    /// Builds the shared rejection event for this result, safe-echoing only the field NAME and
    /// correlation/identity metadata.
    /// </summary>
    /// <returns>A metadata-only <see cref="ProjectCreationRejected"/>.</returns>
    public IRejectionEvent ToRejectionEvent()
    {
        Contracts.Identifiers.ProjectId? projectId = ProjectId is null ? null : new Contracts.Identifiers.ProjectId(ProjectId);
        return CommandType switch
        {
            nameof(UpdateProjectSetup) => new ProjectSetupUpdateRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(ArchiveProject) => new ProjectArchiveRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(SetProjectFolder) => new ProjectReferenceLinkRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ReferenceKind ?? "folder",
                ReferenceId ?? "unknown",
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(LinkFileReference) => new ProjectReferenceLinkRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ReferenceKind ?? "file",
                ReferenceId ?? "unknown",
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(UnlinkFileReference) => new ProjectReferenceUnlinkRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ReferenceKind ?? "file",
                ReferenceId ?? "unknown",
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(LinkMemory) => new ProjectReferenceLinkRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ReferenceKind ?? "memory",
                ReferenceId ?? "unknown",
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(UnlinkMemory) => new ProjectReferenceUnlinkRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ReferenceKind ?? "memory",
                ReferenceId ?? "unknown",
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            nameof(ConfirmProjectResolution) => new ProjectResolutionConfirmationRejected(
                projectId ?? new Contracts.Identifiers.ProjectId("unknown"),
                TenantId ?? string.Empty,
                ToRejectionReason(),
                RejectedField,
                CorrelationId),
            _ => new ProjectCreationRejected(
                TenantId ?? string.Empty,
                ToRejectionReason(),
                RejectedField,
                CorrelationId,
                projectId),
        };
    }

    // Drops a value to null if it carries control or line-separator characters so a rejection result
    // cannot echo unsafe input bytes back to the caller (mirrors Folders SafePassthrough).
    private static string? SafePassthrough(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (char c in value)
        {
            if (c == '\r' || c == '\n' || char.IsControl(c))
            {
                return null;
            }
        }

        return value;
    }

    private static string? SafeReferenceIdentifier(string? value)
    {
        string? trimmed = SafePassthrough(value)?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Length > ProjectCommandValidator.MaxReferenceIdentifierLength
            || !char.IsLetterOrDigit(trimmed[0]))
        {
            return null;
        }

        foreach (char c in trimmed)
        {
            if (!char.IsLetterOrDigit(c) && c is not '_' and not '-')
            {
                return null;
            }
        }

        return trimmed;
    }
}
