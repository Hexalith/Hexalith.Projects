// <copyright file="InMemoryProjectsStateStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Globalization;
using System.Text.Json;

using Hexalith.Projects.Infrastructure;

/// <summary>Optimistic-concurrency state store fake standing in for the Dapr state-store component.</summary>
internal sealed class InMemoryProjectsStateStore : IProjectsStateStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, (string Json, string ETag)> _states = new(StringComparer.Ordinal);

    /// <summary>Gets the stored state keys.</summary>
    public IReadOnlyCollection<string> Keys
    {
        get
        {
            lock (_gate)
            {
                return [.. _states.Keys];
            }
        }
    }

    /// <inheritdoc />
    public Task<ProjectsStateEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_states.TryGetValue($"{storeName}:{key}", out (string Json, string ETag) state)
                ? new ProjectsStateEntry<T>(JsonSerializer.Deserialize<T>(state.Json, DaprProjectProjectionStore.JsonOptions), state.ETag)
                : new ProjectsStateEntry<T>(default, null));
        }
    }

    /// <inheritdoc />
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string? eTag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string stateKey = $"{storeName}:{key}";
        string json = JsonSerializer.Serialize(value, DaprProjectProjectionStore.JsonOptions);
        lock (_gate)
        {
            bool exists = _states.TryGetValue(stateKey, out (string Json, string ETag) current);
            if (exists ? !string.Equals(current.ETag, eTag, StringComparison.Ordinal) : !string.IsNullOrEmpty(eTag))
            {
                return Task.FromResult(false);
            }

            long next = exists ? long.Parse(current.ETag, CultureInfo.InvariantCulture) + 1 : 1;
            _states[stateKey] = (json, next.ToString(CultureInfo.InvariantCulture));
            return Task.FromResult(true);
        }
    }
}
