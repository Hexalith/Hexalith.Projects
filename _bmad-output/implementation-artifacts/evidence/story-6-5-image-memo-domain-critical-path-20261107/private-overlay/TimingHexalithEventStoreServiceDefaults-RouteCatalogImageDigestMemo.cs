using System.Reflection;
using System.Security.Cryptography;

namespace Hexalith.EventStore.ServiceDefaults.Routing;

/// <summary>Owns bounded image copies and their SHA256 results, without retaining image or host admission.</summary>
internal sealed class RouteCatalogImageDigestMemo : IDisposable
{
    private const int MaximumEntries = 32;
    private const int MaximumImageBytes = 16 * 1024 * 1024;
    private const int MaximumRetainedBytes = 64 * 1024 * 1024;
    private readonly object _gate = new();
    private readonly Dictionary<Assembly, (byte[] Bytes, string Digest)> _images = new(ReferenceEqualityComparer.Instance);
    private int _retainedBytes;
    private bool _closed;

    /// <summary>Gets whether its exact owning authority or scope has released this storage.</summary>
    internal bool IsClosed { get { lock (_gate) return _closed; } }

    /// <summary>Gets the retained image count.</summary>
    internal int Count { get { lock (_gate) return _images.Count; } }

    /// <summary>Gets conservative retained bytes, including private arrays, digest strings and dictionary storage.</summary>
    internal int RetainedBytes { get { lock (_gate) return _retainedBytes; } }

    /// <summary>Compares every freshly captured byte after the caller's unchanged PE/MVID validation.</summary>
    internal bool TryCompute(Assembly assembly, ReadOnlySpan<byte> current, out string digest, out bool reused)
    {
        using var __focus26 = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("image.memo.total");
        lock (_gate)
        {
            digest = string.Empty;
            reused = false;
            global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.calls", current.Length);
            if (_closed || current.Length is 0 or > MaximumImageBytes) { global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.reject"); return false; }
            bool present = _images.TryGetValue(assembly, out var previous);
            if (present && current.SequenceEqual(previous.Bytes))
            {
                digest = previous.Digest;
                reused = true;
                global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.hit");
                global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.retained-bytes", _retainedBytes);
                global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.entries", _images.Count);
                return true;
            }
            int replacedBytes = present ? previous.Bytes.Length + 512 : 0;
            int chargedBytes = current.Length + 512;
            if (!present && _images.Count == MaximumEntries || chargedBytes > MaximumRetainedBytes - (_retainedBytes - replacedBytes)) { global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.reject"); return false; }
            // Hash the retained private copy, never a buffer returned later to the image pool.
            global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.miss");
            using var __hash = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("image.memo.hash");
            byte[] owned = current.ToArray();
            digest = Convert.ToHexStringLower(SHA256.HashData(owned));
            __hash?.Dispose();
            _images[assembly] = (owned, digest);
            _retainedBytes += chargedBytes - replacedBytes;
            global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.retained-bytes", _retainedBytes);
            global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counter("image.memo.entries", _images.Count);
            return true;
        }
    }

    /// <summary>Clears only deterministic input storage; every future call uses the ordinary hash path.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _closed = true;
            _images.Clear();
            _retainedBytes = 0;
        }
    }
}
