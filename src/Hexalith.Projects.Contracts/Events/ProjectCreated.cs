// <copyright file="ProjectCreated.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Success event emitted when a Project is durably created (FR-1, AR-3, AR-5, AR-6). Past-tense
/// name, no <c>Event</c> suffix, mirroring the Folders <c>FolderCreated</c> shape.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Carries only the canonical identity, the project name,
/// an optional safe description, optional safe setup-metadata reference, the shared
/// <see cref="ProjectLifecycle"/> (always <see cref="ProjectLifecycle.Active"/> at creation), and
/// actor/correlation/task/idempotency metadata plus the <see cref="OccurredAt"/> instant.
/// <b>No conversation transcript, file content, memory body, raw setup body, folder path, secret, or
/// token is duplicated into the project.</b> The boundary is enforced by the FS-2
/// <c>NoPayloadLeakage</c> harness against
/// <see cref="Hexalith.Projects.Contracts.Models.PayloadClassification.ForbiddenContent"/>.
///
/// <para>
/// <b>Event-catalog entry (AR-6):</b> documented in <c>docs/event-catalog.md</c>. Purpose: records a
/// new active project. Sensitivity: metadata-only. Consumers: <c>ProjectListProjection</c>,
/// <c>ProjectDetailProjection</c>.
/// </para>
/// Schema evolution is additive and serialization-tolerant: never introduce a <c>V2</c> event type;
/// new fields must be optional with backward-compatible deserialization (NFR-6, FS-5).
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The opaque project identifier (ULID-shaped value).</param>
/// <param name="Name">The project name (metadata only).</param>
/// <param name="Description">Optional safe, metadata-only project description.</param>
/// <param name="SetupMetadata">Optional safe, reference-only setup-metadata reference (never a raw body/path/secret).</param>
/// <param name="Lifecycle">The project lifecycle state at creation (always <see cref="ProjectLifecycle.Active"/>).</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record ProjectCreated(
    string TenantId,
    string ProjectId,
    string Name,
    string? Description,
    string? SetupMetadata,
    ProjectLifecycle Lifecycle,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
