namespace Story65.AdvisoryWaveProof;

internal sealed class ProofAssertionFailure(string file, int line) : Exception
{
    internal string AssertionFile { get; } = Path.GetFileName(file);
    internal int AssertionLine { get; } = line;
}
