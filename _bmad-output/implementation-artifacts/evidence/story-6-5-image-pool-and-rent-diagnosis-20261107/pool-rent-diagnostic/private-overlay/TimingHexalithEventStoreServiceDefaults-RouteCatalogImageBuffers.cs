using System.Buffers;

namespace Hexalith.EventStore.ServiceDefaults.Routing;

/// <summary>Shares bounded concurrent image buffers only within an existing open private computation scope.</summary>
internal static class RouteCatalogImageBuffers
{
    // Two arrays per bucket retain less than 64 MiB of payload across sizes up to 16 MiB.
    // Both readers keep their ordinary one-array pools outside the selected scope, for
    // less than 128 MiB of payload across all three pools, separate from digest storage.
    private static readonly ArrayPool<byte> _concurrent = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.WrapPool(ArrayPool<byte>.Create(16 * 1024 * 1024, 2), "pool.private", 2);

    /// <summary>Selects storage without retaining any observation, digest or authority result.</summary>
    internal static ArrayPool<byte> Select(ArrayPool<byte> ordinary)
        { var scope = RouteCatalogImageDigestScope.Current; bool open = scope is { IsOpen: true }; global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.PoolSelection(open, scope is null); return open ? _concurrent : ordinary; }
}
