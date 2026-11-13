namespace Story65.TechnicalFingerprint.Harness;
internal readonly record struct MemoCounters(int Entries, long ChargedBytes, long Hits, long Stores,
    long ConstraintFallbacks, long UnavailableScopeFallbacks, long UnsupportedKeyFallbacks);
