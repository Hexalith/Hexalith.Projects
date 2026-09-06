// <copyright file="InMemoryProjectReadModelStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;

/// <summary>
/// A minimal, thread-safe in-memory <see cref="IReadModelStore"/> default registered by
/// <see cref="ProjectsServerServiceCollectionExtensions.AddProjectsServer"/> so lightweight test
/// hosts resolve named persisted read models (e.g. the Conversation-start projection) without a
/// DAPR sidecar. Runtime hosts replace this with the DAPR-backed store via
/// <see cref="ProjectsServerServiceCollectionExtensions.AddProjectsServerRuntimeInfrastructure"/>.
/// </summary>
public sealed class InMemoryProjectReadModelStore : IReadModelStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private long _etagSequence;

    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return Task.FromResult(_entries.TryGetValue(Compose(storeName, key), out Entry? entry)
            ? new ReadModelEntry<TValue>(JsonSerializer.Deserialize<TValue>(entry.Bytes, JsonOptions), entry.ETag)
            : new ReadModelEntry<TValue>(null, null));
    }

    /// <inheritdoc/>
    public Task SaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _entries[Compose(storeName, key)] = new Entry(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions), NextETag());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(etag);

        string composite = Compose(storeName, key);
        bool exists = _entries.TryGetValue(composite, out Entry? current);
        bool matches = exists ? string.Equals(current!.ETag, etag, StringComparison.Ordinal) : etag.Length == 0;
        if (!matches)
        {
            return Task.FromResult(false);
        }

        _entries[composite] = new Entry(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions), NextETag());
        return Task.FromResult(true);
    }

    private static string Compose(string storeName, string key) => storeName + "\0" + key;

    private string NextETag() => Interlocked.Increment(ref _etagSequence).ToString(CultureInfo.InvariantCulture);

    private sealed record Entry(byte[] Bytes, string ETag);
}
