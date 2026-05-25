// <copyright file="DaprProjectListReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Runtime list read model backed by the durable Dapr projection journal.
/// </summary>
public sealed class DaprProjectListReadModel(IProjectProjectionStore projectionStore) : IProjectListReadModel
{
    private readonly IProjectProjectionStore _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectListItem>> ListAsync(
        string authoritativeTenantId,
        ProjectLifecycle? lifecycleFilter,
        CancellationToken cancellationToken = default)
        => await _projectionStore
            .ListAsync(authoritativeTenantId, lifecycleFilter, cancellationToken)
            .ConfigureAwait(false);
}
