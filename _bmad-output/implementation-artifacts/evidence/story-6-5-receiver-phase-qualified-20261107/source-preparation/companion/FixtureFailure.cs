using System.Runtime.CompilerServices;

namespace Hexalith.PrivateReceiverFixture;

/// <summary>Reports only a closed assertion code and source coordinate.</summary>
public sealed class FixtureFailure : Exception
{
    /// <summary>Creates a closed synthetic fixture failure.</summary>
    public FixtureFailure(string closedCheck, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClosedCheck = closedCheck;
        AssertionFile = Path.GetFileName(file);
        AssertionLine = line;
    }

    /// <summary>Gets the fixed assertion code.</summary>
    public string ClosedCheck { get; }
    /// <summary>Gets the source basename.</summary>
    public string AssertionFile { get; }
    /// <summary>Gets the source line.</summary>
    public int AssertionLine { get; }
}
