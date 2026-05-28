// <copyright file="FileReferenceLinked.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Events;

using System;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Metadata-only success event emitted when an optional File Reference is linked to an active Project
/// (FR-9, FR-11), after the server validated the referenced file through the Folders ACL.
/// </summary>
/// <remarks>
/// <b>Sensitivity class: metadata-only.</b> Records stable Project identity, the stable Folders-owned
/// file/folder reference identity, safe display metadata, and actor/correlation/task/idempotency
/// metadata. It carries no file contents, byte ranges, raw or workspace paths, unrestricted paths,
/// provider payloads, diffs, secrets, tokens, or raw Folders authorization details. It supplements
/// Project Context; it does not clear, replace, satisfy, or auto-create the single Project Folder.
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="FileReferenceId">The Projects-owned opaque, stable file-reference identifier.</param>
/// <param name="FolderId">The owning Folders-owned folder identifier.</param>
/// <param name="FileMetadata">Safe metadata-only file reference display metadata.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier linking the event to its command.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key recorded for replay deduplication.</param>
/// <param name="IdempotencyFingerprint">The canonical idempotency fingerprint of the originating command.</param>
/// <param name="OccurredAt">The wall-clock instant the event was produced.</param>
public sealed record FileReferenceLinked(
    string TenantId,
    string ProjectId,
    string FileReferenceId,
    string FolderId,
    ProjectFileReferenceMetadata FileMetadata,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    DateTimeOffset OccurredAt) : IProjectEvent;
