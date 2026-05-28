// <copyright file="FileReferenceUnlinked.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

/// <summary>
/// Metadata-only success event emitted when an optional File Reference is unlinked from a Project
/// (FR-9, FR-11). It removes the Project-to-file association only.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> It never deletes, removes, archives, reads, mutates, or
/// otherwise changes the underlying file in Hexalith.Folders, and it never removes the single Project
/// Folder reference. It carries only safe Project identity, the stable opaque file-reference identifier,
/// and actor/correlation/task/idempotency metadata.
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="FileReferenceId">The Projects-owned opaque file-reference identifier that was unlinked.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record FileReferenceUnlinked(
    string TenantId,
    string ProjectId,
    string FileReferenceId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
