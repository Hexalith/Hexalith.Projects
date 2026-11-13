namespace Story65.DomainReceiverPhase;
using System.Runtime.CompilerServices;
internal sealed class DriverFailure(string closedCheck,
    [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : Exception
{
    public string ClosedCheck { get; } = closedCheck;
    public string AssertionFile { get; } = Path.GetFileName(file);
    public int AssertionLine { get; } = line;
}
