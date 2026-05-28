// <copyright file="UnlinkMemory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Command to unlink a Memory Reference from a Project (FR-10, FR-11). It removes only the
/// Project-to-memory association. It never calls Hexalith.Memories, never deletes or mutates the
/// underlying Case or any MemoryUnit, and it never touches the single Project Folder or file
/// references.
/// </summary>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier this command targets.</param>
/// <param name="MemoryReferenceId">The opaque Memories case identifier to unlink.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record UnlinkMemory(
    string TenantId,
    ProjectId ProjectId,
    string MemoryReferenceId,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(UnlinkMemory);
}
