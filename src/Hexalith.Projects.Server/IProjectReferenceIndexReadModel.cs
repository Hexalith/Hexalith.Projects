// <copyright file="IProjectReferenceIndexReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Reads the tenant-scoped reverse Project reference index for attachment resolution.</summary>
public interface IProjectReferenceIndexReadModel
{
    /// <summary>Lists Projects that reference at least one presented folder or file identifier.</summary>
    /// <param name="authoritativeTenantId">The authenticated authoritative tenant.</param>
    /// <param name="folderIds">The presented folder identifiers.</param>
    /// <param name="fileReferenceIds">The presented file reference identifiers.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Tenant-scoped Project candidate rows with matching reference-index evidence.</returns>
    Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
        string authoritativeTenantId,
        IReadOnlyCollection<string> folderIds,
        IReadOnlyCollection<string> fileReferenceIds,
        CancellationToken cancellationToken = default);
}
