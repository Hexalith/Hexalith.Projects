// <copyright file="IProjectListReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Reads the tenant-scoped <c>ProjectListProjection</c> for the <c>ListProjects</c> query.
/// </summary>
public interface IProjectListReadModel
{
    /// <summary>Lists projected rows for the authoritative tenant, optionally filtered by lifecycle.</summary>
    /// <param name="authoritativeTenantId">The authenticated authoritative tenant.</param>
    /// <param name="lifecycleFilter">The lifecycle filter, or null for all lifecycle states.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant-scoped list rows.</returns>
    Task<IReadOnlyList<ProjectListItem>> ListAsync(
        string authoritativeTenantId,
        ProjectLifecycle? lifecycleFilter,
        CancellationToken cancellationToken = default);
}
