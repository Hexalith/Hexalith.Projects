using System.Runtime.CompilerServices;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Stops this feasibility run on a closed invariant.</summary>
internal static class Check
{
    internal static void That(bool value, string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!value) throw new DriverFailure(check, file, line);
    }
}
