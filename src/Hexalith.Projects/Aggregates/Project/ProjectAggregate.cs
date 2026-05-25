// <copyright file="ProjectAggregate.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// The single Project aggregate (AR-3). Pure static <c>Handle</c>: validates the command, fails closed
/// on a missing/unauthorized tenant (emitting a rejection reason, never throwing), is
/// idempotency-tolerant (same key + same payload = replay; same key + different payload = conflict),
/// guards against duplicate creation, and otherwise emits exactly one <c>ProjectCreated</c> success
/// event. Persist-then-publish: <c>Handle</c> returns events only and never mutates state, calls Dapr,
/// touches the network/ACL/HTTP, or calls a sibling client (auto-folder is a named anti-pattern,
/// deferred). State mutation happens exclusively in <see cref="ProjectStateApply"/>.
/// </summary>
/// <remarks>
/// Authored as a <c>partial</c> class so later epics add <c>ProjectAggregate.References.cs</c> /
/// <c>.Resolution.cs</c> without churning this file (Epic 1 decomposition guidance).
/// </remarks>
public static partial class ProjectAggregate
{
    /// <summary>
    /// Handles a <see cref="CreateProject"/> command against the current state.
    /// </summary>
    /// <param name="state">The current aggregate state (use <see cref="ProjectState.Empty"/> for a new stream).</param>
    /// <param name="command">The create command.</param>
    /// <param name="occurredAt">The wall-clock instant supplied by the command pipeline's <c>TimeProvider</c>.</param>
    /// <returns>An accepted result with a single <c>ProjectCreated</c> event, or a rejected result carrying a reason.</returns>
    public static ProjectResult Handle(ProjectState state, CreateProject command, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(command);
        if (!validation.IsAccepted)
        {
            return ProjectResult.Rejected(command, validation.Code, validation.RejectedField);
        }

        // Idempotency check BEFORE the duplicate-create guard. Same key already recorded on this stream
        // means the prior create was accepted; an equivalent payload is a logical replay (no second
        // event), a non-equivalent payload is a conflict.
        if (state.IdempotencyFingerprints.TryGetValue(command.IdempotencyKey, out string? priorFingerprint))
        {
            return string.Equals(priorFingerprint, validation.IdempotencyFingerprint, StringComparison.Ordinal)
                ? ProjectResult.Rejected(command, ProjectResultCode.IdempotentReplay)
                : ProjectResult.Rejected(command, ProjectResultCode.IdempotencyConflict);
        }

        if (state.IsCreated)
        {
            return ProjectResult.Rejected(command, ProjectResultCode.DuplicateProject);
        }

        ProjectCreated created = new(
            command.TenantId,
            command.ProjectId.Value,
            validation.CanonicalName!,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            ProjectLifecycle.Active,
            command.ActorPrincipalId,
            command.CorrelationId,
            command.TaskId,
            command.IdempotencyKey,
            validation.IdempotencyFingerprint!,
            occurredAt);

        return ProjectResult.Accepted(command, [created]);
    }

    /// <summary>
    /// Deterministic-timestamp test overload (mirrors Folders). Production callers must always supply
    /// <c>OccurredAt</c> from the pipeline's <c>TimeProvider</c> so events carry real wall-clock
    /// evidence rather than <see cref="DateTimeOffset.MinValue"/>.
    /// </summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The create command.</param>
    /// <returns>The handle result with a <see cref="DateTimeOffset.MinValue"/> timestamp.</returns>
    public static ProjectResult Handle(ProjectState state, CreateProject command)
        => Handle(state, command, DateTimeOffset.MinValue);
}
