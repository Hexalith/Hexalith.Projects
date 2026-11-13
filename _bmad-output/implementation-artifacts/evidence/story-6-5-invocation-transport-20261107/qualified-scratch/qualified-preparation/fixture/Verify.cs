namespace Story65.InvocationFixture;

internal static class Verify
{
    internal static void That(bool condition, string message,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    { if (!condition) throw new FixtureAssertionFailure(file, line); }
}
