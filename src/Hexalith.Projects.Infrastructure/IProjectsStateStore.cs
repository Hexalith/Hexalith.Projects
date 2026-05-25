// <copyright file="IProjectsStateStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// Minimal state-store port used by Dapr-backed projection adapters.
/// </summary>
public interface IProjectsStateStore
{
    /// <summary>Reads a state entry.</summary>
    /// <typeparam name="T">The state value type.</typeparam>
    /// <param name="storeName">The Dapr state-store component name.</param>
    /// <param name="key">The state key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The state entry.</returns>
    Task<ProjectsStateEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default);

    /// <summary>Attempts to save a state entry using optimistic concurrency.</summary>
    /// <typeparam name="T">The state value type.</typeparam>
    /// <param name="storeName">The Dapr state-store component name.</param>
    /// <param name="key">The state key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="eTag">The expected ETag, or null for new state.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the save succeeded; otherwise <see langword="false"/>.</returns>
    Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string? eTag, CancellationToken cancellationToken = default);
}
