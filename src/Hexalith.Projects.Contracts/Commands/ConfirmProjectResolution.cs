// <copyright file="ConfirmProjectResolution.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Command to confirm one Project from a previously presented ambiguous Project-resolution result.
/// </summary>
/// <remarks>
/// The candidate set is validation evidence consumed by the HTTP boundary and is intentionally not
/// part of this command or the emitted event. The command persists only the confirmed choice metadata.
/// </remarks>
/// <param name="TenantId">The managed tenant the target project belongs to.</param>
/// <param name="ProjectId">The confirmed target project identifier.</param>
/// <param name="ConversationId">The conversation identifier being associated.</param>
/// <param name="SourceProjectId">Optional expected current source project identifier.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record ConfirmProjectResolution(
    string TenantId,
    ProjectId ProjectId,
    string ConversationId,
    ProjectId? SourceProjectId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(ConfirmProjectResolution);
}
