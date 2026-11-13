namespace Story65.OutboundAssertionProof;

internal static class Check
{
    internal static void Require(bool condition, string message,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    { if (!condition) throw new ProofAssertionFailure(file, line); }
}
