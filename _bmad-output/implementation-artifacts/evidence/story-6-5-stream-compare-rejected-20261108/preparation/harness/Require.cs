using System.Runtime.CompilerServices;

namespace Story65.ImageMemo.Harness;

/// <summary>Stops the isolated experiment on a closed invariant failure.</summary>
internal static class Require
{
    internal static void That(bool condition, string label,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            throw new ParityFailure(label, file, line);
        }
    }
}
