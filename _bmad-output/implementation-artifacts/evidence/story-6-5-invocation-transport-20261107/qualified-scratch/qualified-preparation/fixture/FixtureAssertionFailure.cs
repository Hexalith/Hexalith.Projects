namespace Story65.InvocationFixture;

public sealed class FixtureAssertionFailure(string file, int line) : Exception
{
    public string AssertionFile { get; } = Path.GetFileName(file);
    public int AssertionLine { get; } = line;
}
