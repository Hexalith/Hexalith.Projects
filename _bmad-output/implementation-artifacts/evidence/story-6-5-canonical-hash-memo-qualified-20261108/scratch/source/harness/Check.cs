using System.Runtime.CompilerServices;
namespace Story65.Canonical.Harness;
/// <summary>Stops a scratch experiment on a bounded invariant.</summary>
internal static class Check
{
    internal static void That(bool value, string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    { if (!value) throw new ClosedFailure(check, file, line); }
}
