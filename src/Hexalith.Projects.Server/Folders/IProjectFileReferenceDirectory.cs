// <copyright file="IProjectFileReferenceDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects-owned ACL boundary for validating an optional File Reference against Folders-owned evidence
/// (Story 2.5). It extends the Story 2.4 Folders boundary: Folders remains the lifecycle, file metadata,
/// content, path-policy, and authorization owner; Projects only asks whether a file is currently usable
/// as a metadata-only reference. Access must never be inferred from request payloads, cached local
/// metadata, or the Project Folder reference alone.
/// </summary>
public interface IProjectFileReferenceDirectory
{
    /// <summary>
    /// Validates that a file can be linked as an optional reference by the active Project, using
    /// metadata-only Folders evidence (never file content bytes).
    /// </summary>
    /// <param name="projectId">The Project whose file reference is being linked.</param>
    /// <param name="folderId">The Folders-owned folder the file lives under.</param>
    /// <param name="workspaceId">The Folders-owned workspace identifier used to address the file.</param>
    /// <param name="filePath">The workspace-root-relative path used to address the file metadata.</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="taskId">The task identifier for task-scoped operations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped validation result.</returns>
    Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
        ProjectId projectId,
        string folderId,
        string workspaceId,
        string filePath,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default);
}
