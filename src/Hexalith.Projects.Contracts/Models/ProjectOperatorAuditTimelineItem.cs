// <copyright file="ProjectOperatorAuditTimelineItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>Metadata-only audit row exposed through operator diagnostics.</summary>
/// <param name="AuditEventId">The deterministic audit event identifier.</param>
/// <param name="OperationType">The safe operation type name.</param>
/// <param name="OccurredAt">The timestamp associated with the audit row.</param>
/// <param name="ActorPrincipalId">The actor/source principal identifier when available.</param>
/// <param name="CorrelationId">The operation correlation identifier.</param>
/// <param name="TaskId">The operation task identifier.</param>
/// <param name="ReferenceKind">The affected reference kind when applicable.</param>
/// <param name="ReferenceId">The affected reference identifier when safe and available.</param>
/// <param name="PreviousState">The previous safe state code when known.</param>
/// <param name="NewState">The new safe state code when known.</param>
/// <param name="ReasonCode">The safe reason/state code when available.</param>
/// <param name="ConversationId">The safe conversation identifier for resolution audit rows.</param>
/// <param name="SourceProjectId">The safe source Project identifier for resolution audit rows.</param>
/// <param name="ProjectionSequence">The projection sequence used as freshness evidence.</param>
public sealed record ProjectOperatorAuditTimelineItem(
    string AuditEventId,
    string OperationType,
    DateTimeOffset OccurredAt,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string? ReferenceKind,
    string? ReferenceId,
    string? PreviousState,
    string? NewState,
    string? ReasonCode,
    string? ConversationId,
    string? SourceProjectId,
    long ProjectionSequence);
