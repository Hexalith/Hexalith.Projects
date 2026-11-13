namespace Story65.TechnicalFingerprint.Candidate;

/// <summary>Scratch participation closes independently of its owner and follows the image-scope region.</summary>
internal sealed class FingerprintScope : IDisposable
{
    private static readonly AsyncLocal<FingerprintScope?> Active = new();
    private readonly object _gate = new();
    private readonly FingerprintMemoOwner _owner;
    private readonly FingerprintScope? _prior;
    private bool _closed;
    private FingerprintScope(FingerprintMemoOwner owner) { _owner = owner; _prior = Active.Value; Active.Value = this; }
    internal static FingerprintScope? Current => Active.Value;
    internal static FingerprintScope Enter(FingerprintMemoOwner owner) => new(owner);
    internal bool IsOpen { get { lock (_gate) return !_closed && !_owner.IsClosed; } }
    internal bool TryGet(string? key, out string digest)
    {
        lock (_gate)
        {
            digest = string.Empty;
            if (_closed) { _owner.UnavailableFallback(); return false; }
            return _owner.TryGet(key, out digest);
        }
    }
    internal void Store(string? key, string digest) { lock (_gate) { if (!_closed) _owner.TryStore(key, digest); } }
    internal void ConstraintFallback() => _owner.ConstraintFallback();
    internal void UnsupportedFallback() => _owner.UnsupportedFallback();
    internal void UnavailableFallback() => _owner.UnavailableFallback();
    public void Dispose()
    {
        lock (_gate) { if (_closed) return; _closed = true; }
        if (ReferenceEquals(Active.Value, this)) Active.Value = _prior;
    }
}
