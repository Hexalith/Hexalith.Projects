// <copyright file="IProjectFolderDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects-owned ACL boundary for validating Folders-owned Project Folder references.
/// </summary>
public interface IProjectFolderDirectory
{
    /// <summary>
    /// Validates that a folder can be referenced by the active Project.
    /// </summary>
    /// <param name="projectId">The Project whose folder reference is being changed.</param>
    /// <param name="folderId">The Folders-owned folder reference identifier.</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped validation result.</returns>
    Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
        ProjectId projectId,
        string folderId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-checks the current state of an existing Project Folder reference against the Folders boundary
    /// at Refresh time (Story 3.4, FR-18). This is a read-side recheck of an already-linked reference; it
    /// does NOT validate a new assignment. The handler maps the safe outcome onto a fresh
    /// <see cref="Hexalith.Projects.Contracts.Ui.ReferenceState"/> that overrides the projection-stored
    /// state when the recheck disagrees.
    /// </summary>
    /// <param name="projectId">The Project whose folder reference is being re-checked.</param>
    /// <param name="folderId">The Folders-owned folder reference identifier already stored on the projection.</param>
    /// <param name="correlationId">The caller/project correlation identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A safe Projects-shaped recheck result (same shape as Validate).</returns>
    Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
        ProjectId projectId,
        string folderId,
        string correlationId,
        CancellationToken cancellationToken = default);
}
