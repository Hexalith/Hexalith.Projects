// <copyright file="ProjectResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;
using System.Collections.Generic;

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
/// <param name="RejectedField">The NAME of the rejected field (never its value), or null.</param>
/// <param name="Events">The emitted success events (empty on rejection).</param>
public sealed record ProjectResult(
    ProjectResultCode Code,
    string? TenantId,
    string? ProjectId,
    string? ActorPrincipalId,
    string? CorrelationId,
    string? TaskId,
    string? IdempotencyKey,
    string? RejectedField,
    IReadOnlyList<IProjectEvent> Events)
{
    /// <summary>Gets a value indicating whether the command was accepted (a success event was emitted).</summary>
    public bool IsAccepted => Code == ProjectResultCode.Created;

    /// <summary>
    /// Gets a value indicating whether the result is a logical idempotent replay (no second event;
    /// the prior command's event already landed).
    /// </summary>
    public bool IsIdempotentReplay => Code == ProjectResultCode.IdempotentReplay;

    /// <summary>Creates an accepted result carrying the emitted success events.</summary>
    /// <param name="command">The create command that produced the events.</param>
    /// <param name="events">The emitted success events.</param>
    /// <returns>An accepted <see cref="ProjectResult"/>.</returns>
    public static ProjectResult Accepted(CreateProject command, IReadOnlyList<IProjectEvent> events)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(events);

        return new(
            ProjectResultCode.Created,
            SafePassthrough(command.TenantId),
            SafePassthrough(command.ProjectId?.Value),
            SafePassthrough(command.ActorPrincipalId),
            SafePassthrough(command.CorrelationId),
            SafePassthrough(command.TaskId),
            SafePassthrough(command.IdempotencyKey),
            null,
            events);
    }

    /// <summary>Creates a rejection result, safe-echoing the command identity fields and the field NAME only.</summary>
    /// <param name="command">The command that was rejected.</param>
    /// <param name="code">The control-flow result code (never <see cref="ProjectResultCode.Created"/>).</param>
    /// <param name="rejectedField">The NAME of the rejected field (never its value), or null.</param>
    /// <returns>A rejected <see cref="ProjectResult"/> carrying no events.</returns>
    public static ProjectResult Rejected(CreateProject command, ProjectResultCode code, string? rejectedField = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new(
            code,
            SafePassthrough(command.TenantId),
            SafePassthrough(command.ProjectId?.Value),
            SafePassthrough(command.ActorPrincipalId),
            SafePassthrough(command.CorrelationId),
            SafePassthrough(command.TaskId),
            SafePassthrough(command.IdempotencyKey),
            rejectedField,
            []);
    }

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
        ProjectResultCode.IdempotencyConflict => ReferenceState.Conflict,
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
    public ProjectCreationRejected ToRejectionEvent()
        => new(
            TenantId ?? string.Empty,
            ToRejectionReason(),
            RejectedField,
            CorrelationId,
            ProjectId is null ? null : new Contracts.Identifiers.ProjectId(ProjectId));

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
}
