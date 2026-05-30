// <copyright file="ProjectAggregate.Resolution.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Resolution-confirmation command handling for the Project aggregate.</summary>
public static partial class ProjectAggregate
{
    /// <summary>
    /// Handles a <see cref="ConfirmProjectResolution"/> command against the current Project state.
    /// </summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The confirm-resolution command.</param>
    /// <param name="occurredAt">The event timestamp supplied by the command pipeline.</param>
    /// <returns>An accepted confirmation result, an idempotent replay, or a rejection.</returns>
    public static ProjectResult Handle(ProjectState state, ConfirmProjectResolution command, DateTimeOffset occurredAt)
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

        ProjectResolutionConfirmed confirmed = new(
            command.TenantId,
            command.ProjectId.Value,
            command.ConversationId.Trim(),
            command.SourceProjectId?.Value,
            command.ActorPrincipalId,
            command.CorrelationId,
            command.TaskId,
            command.IdempotencyKey,
            validation.IdempotencyFingerprint!,
            occurredAt);

        return ProjectResult.Accepted(command, ProjectResultCode.ProjectResolutionConfirmed, [confirmed]);
    }

    /// <summary>Deterministic-timestamp test overload for resolution confirmations.</summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The confirm-resolution command.</param>
    /// <returns>The handle result with a <see cref="DateTimeOffset.MinValue"/> timestamp.</returns>
    public static ProjectResult Handle(ProjectState state, ConfirmProjectResolution command)
        => Handle(state, command, DateTimeOffset.MinValue);
}
