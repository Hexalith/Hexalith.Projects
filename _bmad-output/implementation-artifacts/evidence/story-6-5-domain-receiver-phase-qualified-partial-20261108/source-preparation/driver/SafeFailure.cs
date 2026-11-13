namespace Story65.DomainReceiverPhase;
using System.Reflection;
using System.Text.RegularExpressions;
internal sealed record SafeFailure(string ErrorType, string? ClosedCheck, string? AssertionFile, int? AssertionLine)
{
    internal static SafeFailure From(Exception error)
    {
        while (error is TargetInvocationException { InnerException: { } inner }) error = inner;
        if (error is DriverFailure own) return new("DriverFailure", own.ClosedCheck, own.AssertionFile, own.AssertionLine);
        if (error.GetType().FullName == "Hexalith.PrivateDomainReceiverFixture.FixtureFailure")
        {
            object? Read(string name) => error.GetType().GetProperty(name)?.GetValue(error)
                ?? error.GetType().GetField(name)?.GetValue(error);
            string? closed = Read("ClosedCheck") as string, file = Read("AssertionFile") as string;
            int? line = Read("AssertionLine") as int?;
            return new("FixtureFailure", closed is not null && Regex.IsMatch(closed, "^[A-Za-z0-9-]{1,100}$") ? closed : null,
                file is not null && Regex.IsMatch(Path.GetFileName(file), "^[A-Za-z0-9_.-]{1,100}$") ? Path.GetFileName(file) : null, line);
        }
        return new("UnexpectedClosedFailure", null, null, null); // Never serialize Message/ToString/StackTrace or exception objects.
    }
}
