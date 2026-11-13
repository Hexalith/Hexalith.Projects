namespace Story65.OutboundAssertionProof;

using System.Diagnostics;
using Hexalith.EventStore.Contracts.Routing;

// Synthetic test-only state. The live bootstrap and exact helper never reference this type.
internal static class FlowCanaryState
{
    internal static readonly object Marker = new();
    internal static readonly AsyncLocal<object?> Local = new(static change =>
    {
        if (change.ThreadContextChanged && Thread.CurrentThread.Name == "PrivateCaptureCounterDrain"
            && ReferenceEquals(change.CurrentValue, Marker)) Interlocked.Exchange(ref DrainObserved, 1);
    });
    internal static int DrainObserved;
    internal static int ProbeChecks;
    internal static int FirstProbe = -1;
    internal static int FirstIssuerProbe = -1;

    internal static void AssertCaller(Activity activity, bool suppressed)
    {
        Check.Require(ReferenceEquals(Local.Value, Marker), "Synthetic caller canary changed.");
        Check.Require(ReferenceEquals(Activity.Current, activity), "Synthetic caller Activity changed.");
        Check.Require(ExecutionContext.IsFlowSuppressed() == suppressed, "Caller flow suppression state changed.");
    }

    internal static async Task ProbeAsync()
    {
        bool suppressed = ExecutionContext.IsFlowSuppressed();
        Interlocked.CompareExchange(ref FirstProbe, suppressed ? 1 : 0, -1);
        object? local = suppressed ? null : Local.Value;
        Activity? activity = suppressed ? null : Activity.Current;
        IRouteCatalogRemoteBoundary? boundary = suppressed ? null : RouteCatalogRemoteExecution.Current;
        await Task.Yield();
        Check.Require(ReferenceEquals(Local.Value, local), "Fake await lost or introduced synthetic canary flow.");
        Check.Require(ReferenceEquals(Activity.Current, activity), "Fake await lost or introduced synthetic Activity flow.");
        Check.Require(ReferenceEquals(RouteCatalogRemoteExecution.Current, boundary), "Fake await introduced or lost remote boundary flow.");
        Interlocked.Increment(ref ProbeChecks);
    }
}
