// <copyright file="ProjectCreationAbandoned.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Terminal fence event emitted on a reserved <c>ProjectId</c> stream when Folder-first creation
/// is abandoned before <see cref="ProjectCreated"/> commits (Story 7.1, AD-35). Prevents any late
/// creation commit on the reserved stream and remains invisible in active Project reads.
/// </summary>
/// <param name="TenantId">The managed tenant the reserved project stream belongs to.</param>
/// <param name="ProjectId">The reserved opaque project identifier.</param>
/// <param name="FolderId">The optional Folder identifier whose claim is being released, if any.</param>
/// <param name="FolderClaimGeneration">The optional AD-35 claim generation held when abandoning, if any.</param>
/// <param name="ReasonCode">The Safe Metadata reason code explaining the abandonment.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The Durable Task identifier for the abandoned creation.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the fence event was produced.</param>
public sealed record ProjectCreationAbandoned(
    string TenantId,
    string ProjectId,
    string? FolderId,
    long? FolderClaimGeneration,
    string ReasonCode,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent
{
    /// <summary>Gets the optional deterministic Durable Task step key for the abandonment fence.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaskStepKey { get; init; }
}
