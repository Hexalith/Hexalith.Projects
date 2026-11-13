using System.Runtime.CompilerServices;
namespace Story65.TechnicalFingerprint.Harness;
internal static class Require
{
    internal static void That(bool value, string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!value) throw new FixtureFailure(check, Path.GetFileName(file), line);
    }
}
