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
    public LiveFixtureGraph Add(LiveFixtureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        graph.Validate();
        LiveFixtureGraph current = _graphs.GetOrAdd(graph.GraphId, graph);
        if (current != graph)
        {
            throw new InvalidOperationException($"Fixture graph '{graph.GraphId}' was reused with different metadata.");
        }

        return current;
    }

    /// <summary>Removes one graph without affecting sibling runs.</summary>
    public bool Remove(string graphId) => _graphs.TryRemove(graphId, out _);

    /// <summary>Finds a graph using an exact metadata identity.</summary>
    public LiveFixtureGraph? Find(Func<LiveFixtureGraph, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _graphs.Values.FirstOrDefault(predicate);
    }
}
