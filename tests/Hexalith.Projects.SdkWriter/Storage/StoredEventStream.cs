// <copyright file="StoredEventStream.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Storage;

using System.Collections.Generic;

/// <summary>Persisted event stream snapshot stored in <see cref="Hexalith.EventStore.Client.Projections.IReadModelStore"/>.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="Domain">The owning EventStore domain.</param>
/// <param name="AggregateId">The aggregate stream identifier.</param>
/// <param name="Version">The current stream head version (count of persisted events).</param>
/// <param name="Events">The ordered sequence of persisted events.</param>
public sealed record StoredEventStream(
    string TenantId,
    string Domain,
    string AggregateId,
    long Version,
    IReadOnlyList<StoredStreamEvent> Events);
