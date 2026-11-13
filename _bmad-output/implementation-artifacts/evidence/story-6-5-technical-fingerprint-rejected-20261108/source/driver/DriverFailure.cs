using System.Runtime.CompilerServices;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Exposes only fixed driver invariants and source coordinates.</summary>
internal sealed class DriverFailure(string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : Exception
{
    internal string Check { get; } = check;
    internal string File { get; } = Path.GetFileName(file);
    internal int Line { get; } = line;
}
