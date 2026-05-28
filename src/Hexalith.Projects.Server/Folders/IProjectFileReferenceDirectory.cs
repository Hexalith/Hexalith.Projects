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

    /// <summary>
    /// Re-checks the current state of an existing Project File reference against the Folders boundary
    /// at Refresh time (Story 3.4, FR-18). This is a read-side recheck of an already-linked reference; it
    /// does NOT validate a new link. The Refresh recheck is by opaque file-reference id and stored
    /// folder id — Projects must NEVER store workspaceId / filePath / path-like fields, so the recheck
    /// uses only the safe identifiers already present on the projection. Implementations that cannot
    /// perform an opaque-id recheck (because the upstream stable read route does not yet exist) MUST
    /// fail closed and return <see cref="ProjectFileReferenceValidationOutcome.Unavailable"/>; the
    /// outcome mapper translates that to <see cref="Hexalith.Projects.Contracts.Ui.ReferenceState.Unavailable"/>.
    /// </summary>
    /// <param name="projectId">The Project whose file reference is being re-checked.</param>
    /// <param name="fileReferenceId">The Projects-owned opaque file-reference identifier.</param>
    /// <param name="folderId">The Folders-owned folder identifier the file lives under (from the projection).</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="taskId">The task identifier for task-scoped operations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped recheck result (same shape as Validate).</returns>
    Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
        ProjectId projectId,
        string fileReferenceId,
        string folderId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default);
}
