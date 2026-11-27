// <copyright file="Story71InMemoryReadModelStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Story71;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;

/// <summary>
/// Thread-safe, CAS-enforcing in-memory <see cref="IReadModelStore"/> and <see cref="IReadModelBatchStore"/>
/// for Story 7.1 unit and workflow qualification tests.
/// </summary>
public sealed class Story71InMemoryReadModelStore : IReadModelStore, IReadModelBatchStore, IReadModelPendingBatchRecoveryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _gate = new();
    private readonly Dictionary<string, (byte[] Bytes, string ETag)> _entries = new(StringComparer.Ordinal);
    private long _etagSequence;

    /// <summary>Gets the count of persisted keys.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    /// <inheritdoc/>
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullKey = CompositeKey(storeName, key);
        lock (_gate)
        {
            if (!_entries.TryGetValue(fullKey, out (byte[] Bytes, string ETag) entry))
            {
                return Task.FromResult(new ReadModelEntry<T>(null, null));
            }

            T? value = JsonSerializer.Deserialize<T>(entry.Bytes, JsonOptions);
            return Task.FromResult(new ReadModelEntry<T>(value, entry.ETag));
        }
    }

    /// <inheritdoc/>
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullKey = CompositeKey(storeName, key);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), JsonOptions);
        lock (_gate)
        {
            _entries[fullKey] = (bytes, NextETag());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullKey = CompositeKey(storeName, key);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), JsonOptions);
        lock (_gate)
        {
            bool exists = _entries.TryGetValue(fullKey, out (byte[] Bytes, string ETag) existing);
            if (string.IsNullOrEmpty(etag))
            {
                if (exists)
                {
                    return Task.FromResult(false);
                }
            }
            else if (!exists || !string.Equals(existing.ETag, etag, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _entries[fullKey] = (bytes, NextETag());
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            foreach (ReadModelBatchOperation op in batch.Operations)
            {
                string fullKey = CompositeKey(batch.Scope.StoreName, op.Key);
                bool exists = _entries.TryGetValue(fullKey, out (byte[] Bytes, string ETag) existing);
                if (op.Concurrency.Mode == ReadModelBatchConcurrencyMode.ExpectedETag)
                {
                    if (string.IsNullOrEmpty(op.Concurrency.ExpectedETag))
                    {
                        if (exists)
                        {
                            return Task.FromResult(ReadModelBatchResult.OptimisticConflict(batch.Scope.BatchId, op.Key));
                        }
                    }
                    else if (!exists || !string.Equals(existing.ETag, op.Concurrency.ExpectedETag, StringComparison.Ordinal))
                    {
                        return Task.FromResult(ReadModelBatchResult.OptimisticConflict(batch.Scope.BatchId, op.Key));
                    }
                }
            }

            foreach (ReadModelBatchOperation op in batch.Operations)
            {
                string fullKey = CompositeKey(batch.Scope.StoreName, op.Key);
                if (op.Kind == ReadModelBatchOperationKind.Delete)
                {
                    _entries.Remove(fullKey);
                }
                else
                {
                    _entries[fullKey] = (op.CanonicalValue.ToArray(), NextETag());
                }
            }

            return Task.FromResult(ReadModelBatchResult.Completed(batch.Scope.BatchId));
        }
    }

    /// <inheritdoc/>
    public Task<ReadModelPendingBatchRecoveryResult> RecoverPendingAsync(
        ReadModelBatchScope scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ReadModelPendingBatchRecoveryResult(true));
    }

    /// <summary>Reads a strongly typed snapshot directly for test assertions.</summary>
    /// <typeparam name="T">The stored type.</typeparam>
    /// <param name="storeName">The store component name.</param>
    /// <param name="key">The storage key.</param>
    /// <returns>The deserialized value, or null.</returns>
    public T? Snapshot<T>(string storeName, string key)
        where T : class
    {
        string fullKey = CompositeKey(storeName, key);
        lock (_gate)
        {
            return _entries.TryGetValue(fullKey, out (byte[] Bytes, string ETag) entry)
                ? JsonSerializer.Deserialize<T>(entry.Bytes, JsonOptions)
                : null;
        }
    }

    private string NextETag() => (++_etagSequence).ToString(CultureInfo.InvariantCulture);

    private static string CompositeKey(string storeName, string key) => $"{storeName}||{key}";
}
