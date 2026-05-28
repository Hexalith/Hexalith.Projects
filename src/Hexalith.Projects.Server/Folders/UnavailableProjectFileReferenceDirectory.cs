// <copyright file="UnavailableProjectFileReferenceDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>Fail-closed file-reference directory used when no Folders client is configured.</summary>
internal sealed class UnavailableProjectFileReferenceDirectory : IProjectFileReferenceDirectory
{
    /// <inheritdoc />
    public Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
        ProjectId projectId,
        string folderId,
        string workspaceId,
        string filePath,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));

    /// <inheritdoc />
    public Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
        ProjectId projectId,
        string fileReferenceId,
        string folderId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
}
