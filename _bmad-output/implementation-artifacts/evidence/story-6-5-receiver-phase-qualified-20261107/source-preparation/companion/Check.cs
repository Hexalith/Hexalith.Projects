using System.Runtime.CompilerServices;

namespace Hexalith.PrivateReceiverFixture;

/// <summary>Enforces fixed synthetic expectations without exporting business values.</summary>
internal static class Check
{
    /// <summary>Requires a closed invariant.</summary>
    internal static void That(bool condition, string code, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!condition) throw new FixtureFailure(code, file, line);
    }
}
