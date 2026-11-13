namespace Story65.InvocationTransportProof;
using System.Diagnostics;
internal static class FlowCanaryState
{
    internal static readonly object Marker = new();
    internal static readonly AsyncLocal<object?> Local = new(static change =>
    {
        if (change.ThreadContextChanged && Thread.CurrentThread.Name == "PrivateCaptureCounterDrain" && ReferenceEquals(change.CurrentValue, Marker)) Interlocked.Exchange(ref DrainObserved, 1);
    });
    internal static int DrainObserved, ProbeChecks, FirstProbe = -1;
    internal static void AssertCaller(Activity activity)
    {
        Check.Require(ReferenceEquals(Local.Value, Marker) && ReferenceEquals(Activity.Current, activity) && !ExecutionContext.IsFlowSuppressed(), "Synthetic caller context changed.");
    }
    internal static void Probe(bool suppressed) => Interlocked.CompareExchange(ref FirstProbe, suppressed ? 1 : 0, -1);
    internal static async Task FakeAwaitAsync()
    {
        bool suppressed = ExecutionContext.IsFlowSuppressed(); object? local = suppressed ? null : Local.Value; Activity? activity = suppressed ? null : Activity.Current;
        await Task.Yield();
        Check.Require(ReferenceEquals(Local.Value, local) && ReferenceEquals(Activity.Current, activity), "Synthetic fake await context changed.");
        Interlocked.Increment(ref ProbeChecks);
    }
}
