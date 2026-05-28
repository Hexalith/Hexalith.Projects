// <copyright file="UnlinkFileReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Command to unlink an optional File Reference from a Project (FR-9, FR-11). It removes only the
/// Project-to-file association. It never deletes, removes, archives, reads, mutates, or otherwise
/// changes the underlying file in Hexalith.Folders, and it never touches the single Project Folder.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier this command targets.</param>
/// <param name="FileReferenceId">The Projects-owned opaque file-reference identifier to unlink.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record UnlinkFileReference(
    string TenantId,
    ProjectId ProjectId,
    string FileReferenceId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(UnlinkFileReference);
}
