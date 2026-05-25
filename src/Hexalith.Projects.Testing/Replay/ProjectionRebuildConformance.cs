// <copyright file="ProjectionRebuildConformance.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Replay;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Reusable Tier-1 FS-6 rebuild / replay / idempotency conformance guard for projection read models.
/// The <b>single</b> place every epic proves the load-bearing Epic-1 projection invariants on its event
/// set, mirroring the reuse intent of the Story 1.4
/// <see cref="Hexalith.Projects.Testing.Leakage.NoPayloadLeakageAssertions"/> harness — a reusable guard,
/// not a one-off test.
/// </summary>
/// <remarks>
/// <para>
/// Parameterized over (a) an <see cref="IReadOnlyList{T}"/> of <see cref="ProjectProjectionEnvelope"/>
/// event stream, (b) an <em>empty</em> starting projection, (c) a <em>rebuild</em> delegate that folds a
/// full stream into a projection (the production <c>Rebuild(...)</c> / <c>Empty.Apply(...)</c> entry
/// point), (d) an <em>applyOne</em> delegate that folds a single envelope onto an existing projection
/// (incremental application, i.e. <c>projection.Apply([envelope])</c>), and (e) an <em>extractor</em>
/// projecting the read model to its keyed item dictionary so equivalence is asserted by <b>content</b>
/// (value-equal items per canonical key) rather than reference equality — record equality over a
/// <c>FrozenDictionary</c> reference member would compare references, not contents. Epic 2/4/5 add a new
/// event type to the stream and call the same <see cref="AssertAll"/> entry point rather than re-deriving
/// the proof.
/// </para>
/// <para>
/// <b>Covered matrix (per projection):</b>
/// <list type="bullet">
/// <item><description><b>rebuild == incremental</b>: rebuilding the full stream value-equals folding it one envelope at a time onto the running projection.</description></item>
/// <item><description><b>rebuild is deterministic</b>: rebuilding the same stream twice yields value-equal state.</description></item>
/// <item><description><b>rebuild is order-stable</b>: rebuilding from a reversed (sequence-consistent) enumerable yields the same final state via the <c>(Sequence, IdempotencyKey, IdempotencyFingerprint)</c> tiebreaker.</description></item>
/// <item><description><b>duplicate delivery is idempotent</b>: appending a duplicate of every envelope leaves the rebuilt state value-equal to the de-duplicated stream.</description></item>
/// <item><description><b>out-of-order converges</b>: a deterministic (seedless) permutation of the stream rebuilds to the same final state as the in-order stream.</description></item>
/// </list>
/// Pure Tier-1: no Dapr, Aspire, network, containers, or browser. Folds use only event-carried data, so
/// every assertion is deterministic with no wall-clock / random / GUID.
/// </para>
/// </remarks>
public static class ProjectionRebuildConformance
{
    /// <summary>
    /// Asserts the full FS-6 rebuild / replay / idempotency matrix for a projection over an event stream.
    /// </summary>
    /// <typeparam name="TProjection">The projection read-model type.</typeparam>
    /// <typeparam name="TItem">The projected item type (a value-equatable record).</typeparam>
    /// <param name="stream">The event stream to prove the property on.</param>
    /// <param name="empty">The empty starting projection.</param>
    /// <param name="rebuild">The full-stream rebuild delegate (the production <c>Rebuild(...)</c> entry point).</param>
    /// <param name="applyOne">Folds a single envelope onto an existing projection (incremental application).</param>
    /// <param name="extractItems">Extracts the keyed item dictionary from a projection for content comparison.</param>
    /// <param name="itemComparer">Optional item equality comparer (defaults to value equality).</param>
    /// <exception cref="ProjectionRebuildConformanceException">Thrown when any invariant fails.</exception>
    public static void AssertAll<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        TProjection empty,
        Func<IEnumerable<ProjectProjectionEnvelope>, TProjection> rebuild,
        Func<TProjection, ProjectProjectionEnvelope, TProjection> applyOne,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem>? itemComparer = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(empty);
        ArgumentNullException.ThrowIfNull(rebuild);
        ArgumentNullException.ThrowIfNull(applyOne);
        ArgumentNullException.ThrowIfNull(extractItems);

        IEqualityComparer<TItem> comparer = itemComparer ?? EqualityComparer<TItem>.Default;

        // The single authoritative final state: rebuild from the full stream.
        IReadOnlyDictionary<string, TItem> rebuilt = extractItems(rebuild(stream));

        AssertRebuildEqualsIncremental(stream, empty, applyOne, extractItems, comparer, rebuilt);
        AssertRebuildIsDeterministic(stream, rebuild, extractItems, comparer, rebuilt);
        AssertRebuildIsOrderStable(stream, rebuild, extractItems, comparer, rebuilt);
        AssertDuplicateDeliveryIsIdempotent(stream, rebuild, extractItems, comparer, rebuilt);
        AssertOutOfOrderConverges(stream, rebuild, extractItems, comparer, rebuilt);
    }

    /// <summary>
    /// Asserts two keyed item maps are <b>content</b>-equal: same key set and value-equal items per key.
    /// Order-insensitive (a projection's final state is a set of rows keyed by canonical identity).
    /// </summary>
    /// <typeparam name="TItem">The projected item type.</typeparam>
    /// <param name="expected">The expected keyed items.</param>
    /// <param name="actual">The actual keyed items.</param>
    /// <param name="because">A short reason for the comparison, surfaced on failure.</param>
    /// <param name="itemComparer">Optional item equality comparer (defaults to value equality).</param>
    /// <exception cref="ProjectionRebuildConformanceException">Thrown when the maps differ.</exception>
    public static void AssertContentEqual<TItem>(
        IReadOnlyDictionary<string, TItem> expected,
        IReadOnlyDictionary<string, TItem> actual,
        string because,
        IEqualityComparer<TItem>? itemComparer = null)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        IEqualityComparer<TItem> comparer = itemComparer ?? EqualityComparer<TItem>.Default;

        if (expected.Count != actual.Count)
        {
            throw new ProjectionRebuildConformanceException(
                $"{because}: item count differs (expected {expected.Count}, actual {actual.Count}).");
        }

        foreach (KeyValuePair<string, TItem> entry in expected)
        {
            if (!actual.TryGetValue(entry.Key, out TItem? actualItem))
            {
                throw new ProjectionRebuildConformanceException(
                    $"{because}: missing canonical key '{entry.Key}' in the compared state.");
            }

            if (!comparer.Equals(entry.Value, actualItem))
            {
                throw new ProjectionRebuildConformanceException(
                    $"{because}: item for canonical key '{entry.Key}' is not value-equal between the two states.");
            }
        }
    }

    private static void AssertRebuildEqualsIncremental<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        TProjection empty,
        Func<TProjection, ProjectProjectionEnvelope, TProjection> applyOne,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem> comparer,
        IReadOnlyDictionary<string, TItem> rebuilt)
    {
        // Incremental: thread the running projection through applyOne, folding exactly one envelope at a
        // time onto the accumulated state — the at-least-once-delivery shape a live projector sees. The
        // batch rebuild must equal this one-at-a-time fold (same events -> same state).
        TProjection incremental = empty;
        foreach (ProjectProjectionEnvelope envelope in stream)
        {
            incremental = applyOne(incremental, envelope);
        }

        AssertContentEqual(rebuilt, extractItems(incremental), "rebuild != incremental", comparer);
    }

    private static void AssertRebuildIsDeterministic<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        Func<IEnumerable<ProjectProjectionEnvelope>, TProjection> rebuild,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem> comparer,
        IReadOnlyDictionary<string, TItem> rebuilt)
    {
        IReadOnlyDictionary<string, TItem> again = extractItems(rebuild(stream));
        AssertContentEqual(rebuilt, again, "rebuild is not deterministic (two rebuilds differ)", comparer);
    }

    private static void AssertRebuildIsOrderStable<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        Func<IEnumerable<ProjectProjectionEnvelope>, TProjection> rebuild,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem> comparer,
        IReadOnlyDictionary<string, TItem> rebuilt)
    {
        ProjectProjectionEnvelope[] reversed = stream.Reverse().ToArray();
        IReadOnlyDictionary<string, TItem> fromReversed = extractItems(rebuild(reversed));
        AssertContentEqual(rebuilt, fromReversed, "rebuild is not order-stable (reversed stream differs)", comparer);
    }

    private static void AssertDuplicateDeliveryIsIdempotent<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        Func<IEnumerable<ProjectProjectionEnvelope>, TProjection> rebuild,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem> comparer,
        IReadOnlyDictionary<string, TItem> rebuilt)
    {
        // At-least-once delivery: every envelope is delivered twice. The de-duplicated final state must be
        // value-equal to the single-delivery rebuild (one row per canonical key, no double-count).
        List<ProjectProjectionEnvelope> doubled = new(stream.Count * 2);
        foreach (ProjectProjectionEnvelope envelope in stream)
        {
            doubled.Add(envelope);
            doubled.Add(envelope);
        }

        IReadOnlyDictionary<string, TItem> fromDoubled = extractItems(rebuild(doubled));
        AssertContentEqual(rebuilt, fromDoubled, "duplicate delivery is not idempotent", comparer);
    }

    private static void AssertOutOfOrderConverges<TProjection, TItem>(
        IReadOnlyList<ProjectProjectionEnvelope> stream,
        Func<IEnumerable<ProjectProjectionEnvelope>, TProjection> rebuild,
        Func<TProjection, IReadOnlyDictionary<string, TItem>> extractItems,
        IEqualityComparer<TItem> comparer,
        IReadOnlyDictionary<string, TItem> rebuilt)
    {
        // A deterministic (seedless) permutation: rotate by half the length. This is a non-trivial
        // reordering that is not the identity (for length > 1) and is reproducible — no random/seed, so
        // the assertion stays deterministic. The deterministic ordering inside the fold must collapse it
        // back to the same final state.
        if (stream.Count > 1)
        {
            int pivot = stream.Count / 2;
            ProjectProjectionEnvelope[] rotated = stream.Skip(pivot).Concat(stream.Take(pivot)).ToArray();
            IReadOnlyDictionary<string, TItem> fromRotated = extractItems(rebuild(rotated));
            AssertContentEqual(rebuilt, fromRotated, "out-of-order delivery did not converge", comparer);
        }
    }
}

/// <summary>Thrown by <see cref="ProjectionRebuildConformance"/> when a rebuild/idempotency invariant fails.</summary>
public sealed class ProjectionRebuildConformanceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ProjectionRebuildConformanceException"/> class.</summary>
    /// <param name="message">The metadata-only invariant-violation summary.</param>
    public ProjectionRebuildConformanceException(string message)
        : base($"ProjectionRebuildConformance violation: {message}.")
    {
    }
}
