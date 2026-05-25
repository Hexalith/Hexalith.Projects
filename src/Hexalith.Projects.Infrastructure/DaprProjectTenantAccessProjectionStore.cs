// <copyright file="DaprProjectTenantAccessProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Hexalith.Projects.Projections.TenantAccess;

/// <summary>
/// Dapr state-backed tenant-access projection store.
/// </summary>
public sealed class DaprProjectTenantAccessProjectionStore(
    IProjectsStateStore stateStore,
    ProjectsStateStoreOptions? options = null) : IProjectTenantAccessProjectionStore
{
    private const string KeyPrefix = "projects:tenant-access:";

    private readonly ProjectsStateStoreOptions _options = options ?? ProjectsStateStoreOptions.Default;
    private readonly IProjectsStateStore _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

    /// <inheritdoc/>
    public async Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ProjectsStateEntry<TenantAccessProjectionDocument> entry = await _stateStore
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

        string key = Key(projection.TenantId);
        ProjectsStateEntry<TenantAccessProjectionDocument> entry = await _stateStore
            .GetAsync<TenantAccessProjectionDocument>(_options.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false);

        long currentVersion = entry.Value?.Version ?? 0L;
        if (projection.Version != currentVersion)
        {
            throw new TenantAccessConcurrencyException(projection.TenantId, projection.Version, currentVersion);
        }

        ProjectTenantAccessProjection snapshot = projection.Clone();
        snapshot.Version = currentVersion + 1L;
        TenantAccessProjectionDocument document = new(snapshot, snapshot.Version);
        bool saved = await _stateStore
            .TrySaveAsync(_options.StateStoreName, key, document, entry.ETag, cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            throw new TenantAccessConcurrencyException(projection.TenantId, projection.Version, currentVersion + 1L);
        }
    }

    private static string Key(string tenantId)
        => KeyPrefix + tenantId.Trim();

    private sealed record TenantAccessProjectionDocument(
        ProjectTenantAccessProjection Projection,
        long Version);
}
