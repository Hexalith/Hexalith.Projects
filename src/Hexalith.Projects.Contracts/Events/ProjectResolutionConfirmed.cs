// <copyright file="ProjectResolutionConfirmed.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

/// <summary>
/// Metadata-only success event emitted after an ambiguous Project resolution is explicitly confirmed.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Records only the confirmed target Project, Conversation,
/// optional expected source Project, actor/correlation/task/idempotency metadata, fingerprint, and
/// timestamp. It does not carry candidate scores, ranks, rejected candidate ids, raw resolution input
/// ids, transcripts, file contents, prompts, memory bodies, paths, tokens, or request bodies.
/// </remarks>
/// <param name="TenantId">The managed tenant the target project belongs to.</param>
/// <param name="ProjectId">The confirmed target project identifier.</param>
/// <param name="ConversationId">The conversation identifier associated through Conversations.</param>
/// <param name="SourceProjectId">Optional expected current source project identifier.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record ProjectResolutionConfirmed(
    string TenantId,
    string ProjectId,
    string ConversationId,
    string? SourceProjectId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
