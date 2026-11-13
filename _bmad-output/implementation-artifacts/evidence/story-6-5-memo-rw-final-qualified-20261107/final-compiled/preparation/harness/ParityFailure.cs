namespace Story65.ImageMemo.Harness;
internal sealed class ParityFailure(string check,
    [System.Runtime.CompilerServices.CallerFilePath] string file = "",
    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0) : Exception
{
    public string ClosedCheck { get; } = check;
    public string AssertionFile { get; } = Path.GetFileName(file);
    public int AssertionLine { get; } = line;
}
