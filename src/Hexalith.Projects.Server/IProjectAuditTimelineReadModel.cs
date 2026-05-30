// <copyright file="IProjectAuditTimelineReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Projections.ProjectAuditTimeline;

/// <summary>Reads the tenant-scoped Project audit timeline projection.</summary>
public interface IProjectAuditTimelineReadModel
{
    /// <summary>Lists audit timeline rows for the authoritative tenant, optionally filtered to one Project.</summary>
    /// <param name="authoritativeTenantId">The authenticated authoritative tenant.</param>
    /// <param name="projectId">The optional Project identifier filter.</param>
    /// <param name="limit">The optional maximum number of rows.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant-scoped audit timeline rows.</returns>
    Task<IReadOnlyList<ProjectAuditTimelineItem>> ListAsync(
        string authoritativeTenantId,
        string? projectId,
        int? limit,
        CancellationToken cancellationToken = default);
}
