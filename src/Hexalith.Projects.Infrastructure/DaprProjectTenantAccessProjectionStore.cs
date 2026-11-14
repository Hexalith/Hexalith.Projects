// <copyright file="DaprProjectTenantAccessProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.EventStore.Client.Projections;

/// <summary>
/// Dapr state-backed tenant-access projection store.
/// </summary>
public sealed class DaprProjectTenantAccessProjectionStore(
    IReadModelStore stateStore,
    ProjectsStateStoreOptions? options = null) : IProjectTenantAccessProjectionStore
{
    private const string KeyPrefix = "projects:tenant-access:";

    private readonly ProjectsStateStoreOptions _options = options ?? ProjectsStateStoreOptions.Default;
    private readonly IReadModelStore _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

    /// <inheritdoc/>
    public async Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ReadModelEntry<TenantAccessProjectionDocument> entry = await _stateStore
            .GetAsync<TenantAccessProjectionDocument>(_options.StateStoreName, Key(tenantId), cancellationToken)
            .ConfigureAwait(false);

        if (entry.Value?.Projection is null)
        {
            return null;
        }

        ProjectTenantAccessProjection projection = entry.Value.Projection.Clone();
        projection.Version = entry.Value.Version;
        return projection;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(projection.TenantId);

        _ = await ReadModelWritePolicy.UpdateAsync<TenantAccessProjectionDocument>(
            _stateStore, _options.StateStoreName, Key(projection.TenantId), current =>
            {
                long currentVersion = current?.Version ?? 0L;
                if (projection.Version != currentVersion)
                {
                    throw new TenantAccessConcurrencyException(projection.TenantId, projection.Version, currentVersion);
                }

                ProjectTenantAccessProjection next = projection.Clone();
                next.Version = currentVersion + 1L;
                return new(next, next.Version);
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(string tenantId, Func<ProjectTenantAccessProjection, bool> update, int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(update);
        _ = await ReadModelWritePolicy.UpdateAsync<TenantAccessProjectionDocument>(
            _stateStore, _options.StateStoreName, Key(tenantId), current =>
            {
                ProjectTenantAccessProjection next = current?.Projection?.Clone() ?? new() { TenantId = tenantId };
                long version = current?.Version ?? 0L;
                next.Version = update(next) ? version + 1L : version;
                return new(next, next.Version);
            }, maxAttempts: maxAttempts, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static string Key(string tenantId)
        => KeyPrefix + tenantId.Trim();

}
