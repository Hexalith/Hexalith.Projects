// <copyright file="IProjectTenantAccessProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System.Threading;
using System.Threading.Tasks;

/// <summary>Store abstraction for the tenant-access projection and its dedup evidence.</summary>
public interface IProjectTenantAccessProjectionStore
{
    /// <summary>Gets the projection for a managed tenant, or null when no usable local evidence exists.</summary>
    Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Saves a projection using optimistic-concurrency semantics.</summary>
    Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default);
}
