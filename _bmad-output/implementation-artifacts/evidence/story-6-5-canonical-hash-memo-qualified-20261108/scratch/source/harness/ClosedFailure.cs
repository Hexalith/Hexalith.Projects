using System.Runtime.CompilerServices;
namespace Story65.Canonical.Harness;
/// <summary>Persists only closed invariant labels and source coordinates.</summary>
internal sealed class ClosedFailure(string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : Exception
{
    internal string Check { get; } = check;
    internal string AssertionFile { get; } = Path.GetFileName(file);
    internal int AssertionLine { get; } = line;
}
