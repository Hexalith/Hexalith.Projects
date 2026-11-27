// <copyright file="StoredEventStreamAppendResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Storage;

/// <summary>Result of an optimistic-concurrency stream append on <see cref="ReadModelBackedEventStore"/>.</summary>
/// <param name="Succeeded">Whether the events were atomically appended.</param>
/// <param name="ConcurrencyConflict">Whether the append failed due to expected-version or ETag mismatch.</param>
/// <param name="Stream">The latest observed stream snapshot.</param>
public sealed record StoredEventStreamAppendResult(
    bool Succeeded,
    bool ConcurrencyConflict,
    StoredEventStream Stream);
