// <copyright file="MemoryLinked.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Metadata-only success event emitted when a Memory Reference is linked to an active Project
/// (FR-10, FR-11), after the server validated the referenced Memories <c>Case</c> through the
/// Memories ACL.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Records stable Project identity, the opaque Memories
/// case identifier, optional safe display metadata, and actor/correlation/task/idempotency metadata.
/// It carries no <c>MemoryUnit.Content</c>, <c>ContentBytes</c>, <c>ContentHash</c>,
/// <c>SourceUri</c>, <c>SourceType</c>, <c>IngestedBy</c>, <c>Metadata</c>, embedding material,
/// classification, raw <c>ErrorResponse.Message</c>, <c>Suggestion</c>, raw
/// <c>MemoriesRemoteException.Message</c>, tokens, paths, prompts, transcripts, or
/// Memories-internal tenant identifier as a payload field (the envelope tenant is the trusted
/// source).
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="MemoryReferenceId">The opaque Memories case identifier.</param>
/// <param name="MemoryMetadata">Safe metadata-only memory reference display metadata.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record MemoryLinked(
    string TenantId,
    string ProjectId,
    string MemoryReferenceId,
    ProjectMemoryReferenceMetadata MemoryMetadata,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
