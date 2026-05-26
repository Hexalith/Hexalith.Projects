// <copyright file="UnavailableProjectFolderDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>Fail-closed folder directory used when no Folders client is configured.</summary>
internal sealed class UnavailableProjectFolderDirectory : IProjectFolderDirectory
{
    /// <inheritdoc />
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
        ProjectId projectId,
        string folderId,
        string correlationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
}
