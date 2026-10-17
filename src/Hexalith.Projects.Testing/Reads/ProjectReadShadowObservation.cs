namespace Hexalith.Projects.Testing.Reads;

using System.Text.Json;

/// <summary>Evidence captured from one already-authorized list/open path.</summary>
/// <param name="Payload">The exact canonical metadata response.</param>
/// <param name="ReadModelKeys">Ordered logical identities read by the path.</param>
/// <param name="GlobalPositions">Exact persisted positions corresponding to those identities.</param>
/// <param name="CursorBoundary">The decoded logical page boundary; never opaque ciphertext.</param>
/// <param name="ScopeHash">Digest of the immutable query and authenticated cursor scope.</param>
public sealed record ProjectReadShadowObservation(JsonElement Payload, IReadOnlyList<string> ReadModelKeys,
    IReadOnlyList<long> GlobalPositions, string? CursorBoundary, string ScopeHash);
