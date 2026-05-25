// <copyright file="IProjectEvent.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

using Hexalith.EventStore.Contracts.Events;

/// <summary>
/// Marker contract shared by every Projects state-change (success) event (AR-5, AR-6). Mirrors the
/// Folders <c>IFolderEvent</c> convention and extends the EventStore <see cref="IEventPayload"/> so
/// success events flow through the persist-then-publish pipeline alongside the
/// <see cref="IRejectionEvent"/> rejection events.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Implementations carry only safe identifiers, the shared
/// vocabulary, and correlation/idempotency metadata — never a transcript, file body, memory body,
/// secret, token, command body, or path. The canonical tenant/project identity is carried so the
/// aggregate <c>Apply</c> path and the read-model projections can enforce tenant isolation. The
/// <see cref="OccurredAt"/> instant is supplied by the command pipeline's <c>TimeProvider</c>.
/// Netstandard2.0-safe.
/// </remarks>
public interface IProjectEvent : IEventPayload
{
    /// <summary>Gets the managed tenant identifier (the EventStore envelope tenant).</summary>
    string TenantId { get; }

    /// <summary>Gets the project identifier the event applies to (opaque value, ULID-shaped).</summary>
    string ProjectId { get; }

    /// <summary>Gets the correlation identifier linking the event to its originating command.</summary>
    string CorrelationId { get; }

    /// <summary>Gets the task identifier for task-scoped operations.</summary>
    string TaskId { get; }

    /// <summary>Gets the idempotency key recorded with the event for replay deduplication.</summary>
    string IdempotencyKey { get; }

    /// <summary>Gets the canonical idempotency fingerprint of the originating command.</summary>
    string IdempotencyFingerprint { get; }

    /// <summary>Gets the wall-clock instant the event was produced (pipeline <c>TimeProvider</c>).</summary>
    DateTimeOffset OccurredAt { get; }
}
