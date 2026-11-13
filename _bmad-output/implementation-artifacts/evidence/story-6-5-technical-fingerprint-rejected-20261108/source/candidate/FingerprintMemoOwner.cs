namespace Story65.TechnicalFingerprint.Candidate;

/// <summary>Scratch-only shared owner of bounded immutable keys and successful digest strings.</summary>
internal sealed class FingerprintMemoOwner : IDisposable
{
    internal const int MaximumEntries = 256;
    internal const long MaximumChargedBytes = 2 * 1024 * 1024;
    // Conservative charged allowance for the fixed 64-char digest, strings and entry payload;
    // this is not a measurement of CLR heap bytes. UTF-16 key bytes are charged separately.
    internal const int EntryCharge = 512;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private bool _closed;
    private long _bytes, _hits, _stores, _constraints, _unavailable, _unsupported;

    internal bool IsClosed { get { lock (_gate) return _closed; } }
    internal (int Entries, long ChargedBytes, long Hits, long Stores, long ConstraintFallbacks, long UnavailableScopeFallbacks, long UnsupportedKeyFallbacks) Snapshot()
    { lock (_gate) return (_values.Count, _bytes, _hits, _stores, _constraints, _unavailable, _unsupported); }
    internal static long Charge(string key) => EntryCharge + 2L * key.Length;
    private static bool Supported(string? key) => key is not null && Charge(key) <= MaximumChargedBytes
        && (key.StartsWith("technical-http-request:v1;", StringComparison.Ordinal) || key.StartsWith("technical-http-response:v1;", StringComparison.Ordinal));

    internal bool TryGet(string? key, out string digest)
    {
        digest = string.Empty;
        try
        {
            lock (_gate)
            {
                if (_closed || !Supported(key) || !_values.TryGetValue(key!, out digest!)) return false;
                _hits++; return true;
            }
        }
        catch { digest = string.Empty; return false; }
    }

    internal bool TryStore(string? key, string digest)
    {
        try
        {
            lock (_gate)
            {
                if (_closed || !Supported(key) || digest.Length != 64 || digest.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))) return false;
                if (_values.ContainsKey(key!)) return true;
                long charge = Charge(key!);
                if (_values.Count >= MaximumEntries || charge > MaximumChargedBytes - _bytes) return false;
                _values.Add(key!, digest); _bytes += charge; _stores++; return true;
            }
        }
        catch { return false; }
    }

    internal void ConstraintFallback() { try { lock (_gate) _constraints++; } catch { } }
    internal void UnavailableFallback() { try { lock (_gate) _unavailable++; } catch { } }
    internal void UnsupportedFallback() { try { lock (_gate) _unsupported++; } catch { } }
    public void Dispose()
    {
        lock (_gate) { if (_closed) return; _closed = true; _values.Clear(); _bytes = 0; }
    }
}
