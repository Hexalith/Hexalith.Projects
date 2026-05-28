// <copyright file="UnavailableProjectMemoryDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Memories;

using Hexalith.Projects.Contracts.Identifiers;

/// <summary>Fail-closed memory-reference directory used when no Memories client is configured.</summary>
internal sealed class UnavailableProjectMemoryDirectory : IProjectMemoryDirectory
{
    /// <inheritdoc />
    public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));

    /// <inheritdoc />
    public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));
}
