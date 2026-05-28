// <copyright file="LinkMemory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Commands;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Command to link an authorized Memory Reference (a Hexalith.Memories Case) to an active Project
/// (FR-10, FR-11). It only adds/updates a metadata-only memory-reference association; it never clears,
/// replaces, satisfies, or auto-creates the single Project Folder, and never touches existing file
/// references or conversation links.
/// </summary>
/// <remarks>
/// The owning Memories tenant context is the envelope tenant (server-derived); it is never accepted
/// from the request body. Projects stores only the opaque Memories <c>Case.Id</c> and safe display
/// metadata; it never stores any <c>MemoryUnit</c> content, content hash, source URI, embedding
/// material, or raw upstream error text.
/// </remarks>
/// <param name="TenantId">The managed tenant the project belongs to.</param>
/// <param name="ProjectId">The project identifier this command targets.</param>
/// <param name="MemoryReferenceId">The opaque Memories case identifier (ULID-shaped sibling identifier).</param>
/// <param name="MemoryMetadata">Safe metadata-only memory reference display metadata.</param>
/// <param name="ActorPrincipalId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracing.</param>
/// <param name="TaskId">The task identifier for task-scoped operations.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
public sealed record LinkMemory(
    string TenantId,
    ProjectId ProjectId,
    string MemoryReferenceId,
    ProjectMemoryReferenceMetadata MemoryMetadata,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey) : IProjectCommand
{
    /// <summary>Gets the command type discriminator.</summary>
    public string CommandType => nameof(LinkMemory);
}
