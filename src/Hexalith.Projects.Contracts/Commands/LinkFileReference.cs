// <copyright file="LinkFileReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Command to link an authorized optional File Reference to an active Project (FR-9, FR-11). It only
/// adds/updates a metadata-only file-reference association; it never clears, replaces, satisfies, or
/// auto-creates the single Project Folder.
/// </summary>
/// <remarks>
/// The Folders file-metadata addressing tuple (workspace + workspace-relative path) needed by the
/// server-side Folders ACL is supplied on the HTTP request and consumed transiently before dispatch; it
/// is intentionally NOT part of this command, the emitted event, the persisted state, or the projections
/// (Projects stores only the opaque reference id, owning folder id, and safe display metadata).
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier this command targets.</param>
/// <param name="FileReferenceId">The Projects-owned opaque, stable file-reference identifier.</param>
/// <param name="FolderId">The owning Folders-owned folder identifier. Projects stores it as a sibling reference string.</param>
/// <param name="FileMetadata">Safe metadata-only file reference display metadata.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record LinkFileReference(
    string TenantId,
    ProjectId ProjectId,
    string FileReferenceId,
    string FolderId,
    ProjectFileReferenceMetadata FileMetadata,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(LinkFileReference);
}
