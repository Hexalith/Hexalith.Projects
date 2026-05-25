// <copyright file="DaprProjectsStateStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Dapr;
using Dapr.Client;

/// <summary>
/// Dapr state-store implementation for projection and dedup state.
/// </summary>
public sealed class DaprProjectsStateStore(DaprClient client) : IProjectsStateStore
{
    private static readonly StateOptions StateOptions = new()
    {
        Consistency = ConsistencyMode.Strong,
        Concurrency = ConcurrencyMode.FirstWrite,
    };

    private readonly DaprClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc/>
    public async Task<ProjectsStateEntry<T>> GetAsync<T>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        StateEntry<T?> entry = await _client
            .GetStateEntryAsync<T?>(
                storeName,
                key,
                ConsistencyMode.Strong,
                metadata: null,
                cancellationToken)
            .ConfigureAwait(false);

        return new ProjectsStateEntry<T>(entry.Value, entry.ETag);
    }

    /// <inheritdoc/>
    public async Task<bool> TrySaveAsync<T>(
        string storeName,
        string key,
        T value,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (string.IsNullOrWhiteSpace(eTag))
        {
            await _client
                .SaveStateAsync(storeName, key, value, StateOptions, metadata: null, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        return await _client
            .TrySaveStateAsync(storeName, key, value, eTag, StateOptions, metadata: null, cancellationToken)
            .ConfigureAwait(false);
    }
}
