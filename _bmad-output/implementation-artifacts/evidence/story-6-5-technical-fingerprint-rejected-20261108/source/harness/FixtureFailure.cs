namespace Story65.TechnicalFingerprint.Harness;
internal sealed class FixtureFailure(string check, string file, int line) : Exception(check)
{
    internal string Check { get; } = check;
    internal string File { get; } = file;
    internal int Line { get; } = line;
}
