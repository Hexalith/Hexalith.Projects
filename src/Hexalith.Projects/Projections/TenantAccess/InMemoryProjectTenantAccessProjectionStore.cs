// <copyright file="InMemoryProjectTenantAccessProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>In-memory optimistic-concurrency implementation used by tests and explicit pre-runtime fakes.</summary>
public sealed class InMemoryProjectTenantAccessProjectionStore : IProjectTenantAccessProjectionStore
{
    private readonly ConcurrentDictionary<string, ProjectTenantAccessProjection> _projections = new(StringComparer.Ordinal);
    private readonly object _saveLock = new();

    /// <inheritdoc/>
    public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        _ = _projections.TryGetValue(tenantId, out ProjectTenantAccessProjection? projection);
        return Task.FromResult(projection?.Clone());
    }

    /// <inheritdoc/>
    public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(projection.TenantId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_saveLock)
        {
            long currentVersion = _projections.TryGetValue(projection.TenantId, out ProjectTenantAccessProjection? existing)
                ? existing.Version
                : 0L;

            if (projection.Version != currentVersion)
            {
                throw new TenantAccessConcurrencyException(projection.TenantId, projection.Version, currentVersion);
            }

            ProjectTenantAccessProjection snapshot = projection.Clone();
            snapshot.Version = currentVersion + 1L;
            _projections[projection.TenantId] = snapshot;
        }

        return Task.CompletedTask;
    }
}
