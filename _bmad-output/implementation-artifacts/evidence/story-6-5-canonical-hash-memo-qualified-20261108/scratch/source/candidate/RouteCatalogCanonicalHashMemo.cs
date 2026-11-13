namespace Story65.Canonical.Candidate;

/// <summary>Owns only complete immutable serialized-preimage strings and their pure canonical SHA results.</summary>
internal sealed class RouteCatalogCanonicalHashMemo : IDisposable
{
    internal const int MaximumEntries = 512;
    internal const int MaximumRetainedCharge = 4 * 1024 * 1024;
    private const int EntryOverheadCharge = 128;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal);
    private int _retainedCharge;
    private bool _closed;
    internal bool IsClosed { get { lock (_gate) return _closed; } }
    internal int Count { get { lock (_gate) return _digests.Count; } }
    internal int RetainedCharge { get { lock (_gate) return _retainedCharge; } }
    internal int Hits { get; private set; }
    internal int Misses { get; private set; }
    internal bool TryGet(string ownedKey, out string digest)
    {
        lock (_gate)
        {
            digest = string.Empty;
            if (_closed) return false;
            if (_digests.TryGetValue(ownedKey, out string? found))
            {
                Hits++;
                digest = found;
                return true;
            }
            Misses++;
            return false;
        }
    }
    internal void Remember(string ownedKey, string digest)
    {
        lock (_gate)
        {
            if (_closed || _digests.Count >= MaximumEntries || _digests.ContainsKey(ownedKey)) return;
            // Charge UTF-16 payloads, digest and a conservative fixed per-entry allowance.
            // This is a retained-charge policy, not a claim to measure CLR heap object overhead.
            long charge = EntryOverheadCharge + 2L * (ownedKey.Length + digest.Length);
            if (charge > MaximumRetainedCharge - _retainedCharge) return;
            _digests.Add(ownedKey, digest);
            _retainedCharge += (int)charge;
        }
    }
    public void Dispose()
    {
        lock (_gate)
        {
            _closed = true;
            _digests.Clear();
            _retainedCharge = 0;
            Hits = 0;
            Misses = 0;
        }
    }
}
