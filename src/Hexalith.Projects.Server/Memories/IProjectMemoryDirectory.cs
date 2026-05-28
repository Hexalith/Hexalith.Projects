// <copyright file="IProjectMemoryDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Memories;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects-owned ACL boundary for validating a Memory Reference against Hexalith.Memories-owned
/// evidence (Story 2.7). It calls only the stable <c>MemoriesClient.GetCaseAsync</c> read route per the
/// Story 2.6 ADR (<c>docs/adr/memories-link-target.md</c>): Memories remains the lifecycle, content,
/// classification, and authorization owner; Projects only asks whether a Memories <c>Case</c> is
/// currently usable as a metadata-only reference. Access must never be inferred from request payloads,
/// cached local metadata, or the Project Folder reference alone.
/// </summary>
public interface IProjectMemoryDirectory
{
    /// <summary>
    /// Validates that a Hexalith.Memories <c>Case</c> can be linked as a memory reference by the active
    /// Project, using metadata-only Memories evidence (never MemoryUnit content, embeddings, or source
    /// payloads).
    /// </summary>
    /// <param name="projectId">The Project whose memory reference is being linked.</param>
    /// <param name="memoryReferenceId">The opaque Memories case identifier (ULID-shaped).</param>
    /// <param name="tenantId">The envelope tenant identifier (server-derived, never from request body).</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="taskId">The task identifier for task-scoped operations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped validation result.</returns>
    Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default);
}
