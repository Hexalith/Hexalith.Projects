// <copyright file="MemoryUnlinked.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

/// <summary>
/// Metadata-only success event emitted when a Memory Reference is unlinked from a Project
/// (FR-10, FR-11). It removes the Project-to-memory association only.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> It never calls Hexalith.Memories, never deletes or
/// mutates the underlying <c>Case</c> or any <c>MemoryUnit</c>, and never removes the Project
/// Folder or any File Reference. It carries only safe Project identity, the opaque Memories case
/// identifier, and actor/correlation/task/idempotency metadata.
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="MemoryReferenceId">The opaque Memories case identifier that was unlinked.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record MemoryUnlinked(
    string TenantId,
    string ProjectId,
    string MemoryReferenceId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
