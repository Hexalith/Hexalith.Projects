// <copyright file="DaprProjectAuditTimelineReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.ProjectAuditTimeline;

/// <summary>Runtime audit timeline read model backed by the durable Dapr projection journal.</summary>
public sealed class DaprProjectAuditTimelineReadModel(IProjectProjectionStore projectionStore) : IProjectAuditTimelineReadModel
{
    private readonly IProjectProjectionStore _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectAuditTimelineItem>> ListAsync(
        string authoritativeTenantId,
        string? projectId,
        int? limit,
        CancellationToken cancellationToken = default)
        => await _projectionStore
            .ListAuditTimelineAsync(authoritativeTenantId, projectId, limit, cancellationToken)
            .ConfigureAwait(false);
}
