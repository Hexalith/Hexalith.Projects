namespace Story65.Canonical.Harness;
/// <summary>A test-only serialization observation proves misses never serialize mutable original input twice.</summary>
internal sealed class ChangingPreimage
{
    private int _reads;
    public int Value => ++_reads;
    internal int Reads => _reads;
}
