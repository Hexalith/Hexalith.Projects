// <copyright file="DaprProjectReferenceIndexReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

/// <summary>Runtime reverse reference-index read model backed by the durable Dapr projection journal.</summary>
public sealed class DaprProjectReferenceIndexReadModel(IProjectProjectionStore projectionStore) : IProjectReferenceIndexReadModel
{
    private readonly IProjectProjectionStore _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
        string authoritativeTenantId,
        IReadOnlyCollection<string> folderIds,
        IReadOnlyCollection<string> fileReferenceIds,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProjectReferenceIndexItem> references = await _projectionStore
            .ListReferencesByReferenceAsync(authoritativeTenantId, folderIds, fileReferenceIds, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<ProjectListItem> listRows = await _projectionStore
            .ListAsync(authoritativeTenantId, lifecycleFilter: null, cancellationToken)
            .ConfigureAwait(false);

        return ProjectReferenceIndexReadModelMapper.ToCandidateRows(authoritativeTenantId, listRows, references);
    }
}
