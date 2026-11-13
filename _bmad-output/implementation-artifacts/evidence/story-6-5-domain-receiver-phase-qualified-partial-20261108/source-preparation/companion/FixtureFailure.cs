namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Reports only a closed assertion label and its source coordinate.</summary>
public sealed class FixtureFailure(string closedCheck, string assertionFile, int assertionLine) : Exception
{
    /// <summary>Gets a fixed assertion code.</summary>
    public string ClosedCheck { get; } = closedCheck;
    /// <summary>Gets the fixture source basename.</summary>
    public string AssertionFile { get; } = assertionFile;
    /// <summary>Gets the fixture source line.</summary>
    public int AssertionLine { get; } = assertionLine;
}
