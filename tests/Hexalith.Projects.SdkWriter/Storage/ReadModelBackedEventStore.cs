// <copyright file="ReadModelBackedEventStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Storage;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.SdkWriter.Claims;

/// <summary>
/// Shared-store optimistic-concurrency event store backed by <see cref="IReadModelStore"/>.
/// Allocates strictly monotonic <c>GlobalPosition</c> values in the shared store (never process-local)
/// so multiple replicas and restarts never collide.
/// </summary>
public sealed class ReadModelBackedEventStore
{
    private const int MaxGlobalPositionCasAttempts = 64;
    private const string GlobalPositionKey = "story71:global-position:v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> KnownEventTypes =
        typeof(IProjectEvent).Assembly.GetTypes()
            .Where(static t => !t.IsAbstract && !t.IsInterface && (typeof(IProjectEvent).IsAssignableFrom(t) || typeof(IRejectionEvent).IsAssignableFrom(t)))
            .Concat(
            [
                typeof(ProjectFolderClaimAcquired),
                typeof(ProjectFolderClaimReleased),
                typeof(ProjectFolderClaimClosed),
                typeof(ProjectFolderClaimRejected),
            ])
            .Distinct()
            .ToDictionary(static t => t.FullName!, StringComparer.Ordinal);

    private readonly IReadModelStore _store;
    private readonly string _storeName;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="ReadModelBackedEventStore"/> class.</summary>
    /// <param name="store">The conditional state store.</param>
    /// <param name="storeName">The Dapr/platform state-store component name.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    public ReadModelBackedEventStore(IReadModelStore store, string storeName, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _storeName = !string.IsNullOrWhiteSpace(storeName) ? storeName : throw new ArgumentException("Store name is required.", nameof(storeName));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Builds the collision-safe length-prefixed storage key for a stream.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="domain">The EventStore domain name.</param>
    /// <param name="aggregateId">The aggregate stream identifier.</param>
    /// <returns>The storage key.</returns>
    public static string StreamKey(string tenantId, string domain, string aggregateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        return $"story71:streams:v1:{tenantId.Length}:{tenantId}:{domain.Length}:{domain}:{aggregateId.Length}:{aggregateId}";
    }

    /// <summary>Reads the latest stream snapshot (empty at version 0 when absent).</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="domain">The domain name.</param>
    /// <param name="aggregateId">The aggregate stream identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted stream snapshot.</returns>
    public async Task<StoredEventStream> ReadStreamAsync(
        string tenantId,
        string domain,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        string key = StreamKey(tenantId, domain, aggregateId);
        ReadModelEntry<StoredEventStream> entry = await _store
            .GetAsync<StoredEventStream>(_storeName, key, cancellationToken)
            .ConfigureAwait(false);
        return entry.Value ?? new StoredEventStream(tenantId, domain, aggregateId, 0, []);
    }

    /// <summary>Attempts to atomically append events at <paramref name="expectedVersion"/> using CAS.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="domain">The domain name.</param>
    /// <param name="aggregateId">The aggregate stream identifier.</param>
    /// <param name="expectedVersion">The expected current stream version.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="actorId">The authenticated actor principal identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="messageId">The idempotency/message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The append outcome and latest stream snapshot.</returns>
    public async Task<StoredEventStreamAppendResult> TryAppendAsync(
        string tenantId,
        string domain,
        string aggregateId,
        long expectedVersion,
        IReadOnlyList<IEventPayload> events,
        string actorId,
        string correlationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);

        string key = StreamKey(tenantId, domain, aggregateId);
        ReadModelEntry<StoredEventStream> entry = await _store
            .GetAsync<StoredEventStream>(_storeName, key, cancellationToken)
            .ConfigureAwait(false);

        StoredEventStream current = entry.Value ?? new StoredEventStream(tenantId, domain, aggregateId, 0, []);
        if (current.Version != expectedVersion)
        {
            return new StoredEventStreamAppendResult(false, true, current);
        }

        if (events.Count == 0)
        {
            return new StoredEventStreamAppendResult(true, false, current);
        }

        long firstGlobalPosition = await AllocateGlobalPositionsAsync(events.Count, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = _timeProvider.GetUtcNow();
        List<StoredStreamEvent> updatedEvents = [.. current.Events];
        for (int index = 0; index < events.Count; index++)
        {
            IEventPayload payload = events[index];
            long sequence = current.Version + index + 1;
            long globalPosition = firstGlobalPosition + index;
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), JsonOptions);
            updatedEvents.Add(new StoredStreamEvent(
                sequence,
                globalPosition,
                payload.GetType().FullName!,
                serialized,
                now,
                actorId,
                correlationId,
                messageId));
        }

        StoredEventStream next = new(tenantId, domain, aggregateId, current.Version + events.Count, updatedEvents);
        string etag = entry.Value is null ? string.Empty : (entry.ETag ?? string.Empty);
        bool saved = await _store.TrySaveAsync(_storeName, key, next, etag, cancellationToken).ConfigureAwait(false);
        if (saved)
        {
            return new StoredEventStreamAppendResult(true, false, next);
        }

        StoredEventStream refreshed = await ReadStreamAsync(tenantId, domain, aggregateId, cancellationToken).ConfigureAwait(false);
        return new StoredEventStreamAppendResult(false, true, refreshed);
    }

    /// <summary>Deserializes a persisted <see cref="StoredStreamEvent"/> back to its concrete CLR event payload.</summary>
    /// <param name="stored">The stored event envelope.</param>
    /// <returns>The deserialized event payload.</returns>
    public static IEventPayload DeserializeEvent(StoredStreamEvent stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (!KnownEventTypes.TryGetValue(stored.EventTypeName, out Type? eventType))
        {
            throw new InvalidOperationException($"Unknown persisted event type '{stored.EventTypeName}'.");
        }

        return JsonSerializer.Deserialize(stored.Payload, eventType, JsonOptions) as IEventPayload
            ?? throw new InvalidOperationException($"Failed to deserialize persisted event '{stored.EventTypeName}'.");
    }

    private async Task<long> AllocateGlobalPositionsAsync(int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        for (int attempt = 0; attempt < MaxGlobalPositionCasAttempts; attempt++)
        {
            ReadModelEntry<GlobalPositionCounterState> entry = await _store
                .GetAsync<GlobalPositionCounterState>(_storeName, GlobalPositionKey, cancellationToken)
                .ConfigureAwait(false);
            long current = entry.Value?.LastAllocated ?? 0;
            long start = checked(current + 1);
            long end = checked(current + count);
            string etag = entry.Value is null ? string.Empty : (entry.ETag ?? string.Empty);
            if (await _store.TrySaveAsync(_storeName, GlobalPositionKey, new GlobalPositionCounterState(end), etag, cancellationToken).ConfigureAwait(false))
            {
                return start;
            }
        }

        throw new InvalidOperationException("Shared global position allocation exceeded CAS retry budget.");
    }
}
