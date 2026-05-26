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
}
