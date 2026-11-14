// <copyright file="IProjectTenantAccessProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Store abstraction for the tenant-access projection and its dedup evidence.</summary>
public interface IProjectTenantAccessProjectionStore
{
    /// <summary>Gets the projection for a managed tenant, or null when no usable local evidence exists.</summary>
    Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reloads and atomically applies an idempotent mutation with platform-owned bounded conflict retries.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="update">Mutates detached current evidence and returns whether its contents changed.</param>
    /// <param name="maxAttempts">The conditional persistence attempt budget.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(string tenantId, Func<ProjectTenantAccessProjection, bool> update, int maxAttempts,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This read-only tenant-access store does not support atomic updates.");

    /// <summary>Saves a projection using optimistic-concurrency semantics.</summary>
    Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default);
}
