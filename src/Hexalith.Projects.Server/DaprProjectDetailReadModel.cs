// <copyright file="DaprProjectDetailReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>
/// Runtime detail read model backed by the durable Dapr projection journal.
/// </summary>
public sealed class DaprProjectDetailReadModel(IProjectProjectionStore projectionStore) : IProjectDetailReadModel
{
    private readonly IProjectProjectionStore _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <inheritdoc/>
    public async Task<ProjectDetailItem?> GetAsync(
        string authoritativeTenantId,
        string projectId,
        CancellationToken cancellationToken = default)
        => await _projectionStore
            .GetDetailAsync(authoritativeTenantId, projectId, cancellationToken)
            .ConfigureAwait(false);
}
