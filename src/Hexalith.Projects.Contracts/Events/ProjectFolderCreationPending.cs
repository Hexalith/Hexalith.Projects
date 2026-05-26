// <copyright file="ProjectFolderCreationPending.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

/// <summary>
/// Metadata-only degraded event emitted when Project Folder auto-create is queued or flagged.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="DisplayNameIntent">The safe display-name intent to use when folder create becomes available.</param>
/// <param name="ReasonCode">Stable metadata-only reason code explaining why creation is pending.</param>
/// <param name="Retryable">Whether a reconciler may retry when the Folders create path becomes available.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">A stable derived idempotency key for the pending folder-create intent.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the pending intent.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record ProjectFolderCreationPending(
    string TenantId,
    string ProjectId,
    string DisplayNameIntent,
    string ReasonCode,
    bool Retryable,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
