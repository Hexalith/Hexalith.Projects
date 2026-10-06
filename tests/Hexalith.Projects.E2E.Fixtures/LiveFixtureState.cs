// <copyright file="LiveFixtureState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Collections.Concurrent;

/// <summary>Run-scoped, process-local metadata fixture state.</summary>
public sealed class LiveFixtureState
{
    private readonly ConcurrentDictionary<string, LiveFixtureGraph> _graphs = new(StringComparer.Ordinal);

    /// <summary>Gets a stable snapshot of the currently provisioned graphs.</summary>
    public IReadOnlyCollection<LiveFixtureGraph> Graphs => [.. _graphs.Values];

    /// <summary>Adds a graph idempotently.</summary>
    /// <param name="graph">The validated metadata-only graph.</param>
    /// <returns>
    /// <see langword="true"/> when the graph was added or an identical graph already exists;
    /// <see langword="false"/> when the graph identity is reused with different metadata.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the graph metadata is invalid.</exception>
    public bool TryAdd(LiveFixtureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.IsValid())
        {
            throw new ArgumentException("The fixture graph metadata is invalid.", nameof(graph));
        }

        return _graphs.GetOrAdd(graph.GraphId, graph) == graph;
    }

    /// <summary>Removes one graph without affecting sibling runs.</summary>
    /// <param name="graphId">The graph identity.</param>
    /// <returns><see langword="true"/> when the graph existed.</returns>
    public bool Remove(string graphId) => _graphs.TryRemove(graphId, out _);

    /// <summary>Finds a graph using an exact metadata identity.</summary>
    /// <param name="predicate">The exact-match predicate.</param>
    /// <returns>The first matching graph, or <see langword="null"/>.</returns>
    public LiveFixtureGraph? Find(Func<LiveFixtureGraph, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _graphs.Values.FirstOrDefault(predicate);
    }
}
