namespace Story65.InvocationTransportProof;
using System.Reflection;
using System.Runtime.Loader;
internal sealed class SafeFailure(string kind, string? file = null, int? line = null) : Exception
{
    internal string Kind { get; } = kind;
    internal string? AssertionFile { get; } = file;
    internal int? AssertionLine { get; } = line;
    internal static SafeFailure From(Exception error)
    {
        if (error is SafeFailure safe) return safe;
        if (error is ProofAssertionFailure local) return new("ProofAssertionFailure", local.AssertionFile, local.AssertionLine);
        if (error.GetType().FullName == "Story65.InvocationFixture.FixtureAssertionFailure")
            return new("FixtureAssertionFailure", Path.GetFileName(error.GetType().GetProperty("AssertionFile")!.GetValue(error) as string), (int?)error.GetType().GetProperty("AssertionLine")!.GetValue(error));
        // Never copy exception messages or arbitrary exception properties into receipts.
        return new("UnexpectedFixtureFailure");
    }
}
