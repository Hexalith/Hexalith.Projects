// <copyright file="ProjectAuditTimelineItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectAuditTimeline;

/// <summary>
/// Metadata-only audit timeline row for a Project lifecycle or context-reference change.
/// </summary>
/// <param name="TenantId">The authoritative tenant identifier.</param>
/// <param name="ProjectId">The Project identifier.</param>
/// <param name="AuditEventId">A deterministic metadata-only audit row identifier.</param>
/// <param name="OperationType">The stable operation name.</param>
/// <param name="OccurredAt">The event occurrence timestamp.</param>
/// <param name="ActorPrincipalId">The actor principal identifier when carried by the event.</param>
/// <param name="CorrelationId">The command correlation identifier.</param>
/// <param name="TaskId">The task identifier.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
/// <param name="ReferenceKind">The affected reference kind, when the operation affects one.</param>
/// <param name="ReferenceId">The affected reference identifier, when safe and available.</param>
/// <param name="PreviousState">The previous safe lifecycle/reference state, when known.</param>
/// <param name="NewState">The new safe lifecycle/reference state, when known.</param>
/// <param name="ReasonCode">The safe reason code, when available.</param>
/// <param name="ConversationId">The confirmed conversation identifier for resolution operations.</param>
/// <param name="SourceProjectId">The safe source Project identifier for resolution operations.</param>
/// <param name="Sequence">The projection/global-position sequence used for deterministic ordering.</param>
public sealed record ProjectAuditTimelineItem(
    string TenantId,
    string ProjectId,
    string AuditEventId,
    string OperationType,
    DateTimeOffset OccurredAt,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string? ReferenceKind,
    string? ReferenceId,
    string? PreviousState,
    string? NewState,
    string? ReasonCode,
    string? ConversationId,
    string? SourceProjectId,
    long Sequence);
