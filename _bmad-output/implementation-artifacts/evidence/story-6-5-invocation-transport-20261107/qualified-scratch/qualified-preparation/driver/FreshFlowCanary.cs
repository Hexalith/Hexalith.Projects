namespace Story65.InvocationTransportProof;
using System.Diagnostics;
using System.Text.Json;
internal static class FreshFlowCanary
{
    internal static async Task<int> RunAsync(string[] args, SdkVariant candidate)
    {
        string mode = args[0], receipt = args[2];
        Check.Require(mode is "canary-positive-control" or "canary-current" or "canary-suppressed", "Closed canary mode required.");
        using Activity activity = new("synthetic-invocation-canary"); activity.Start(); FlowCanaryState.Local.Value = FlowCanaryState.Marker; FlowCanaryState.AssertCaller(activity);
        // No helper type/fields/ready inspection before this first business invocation.
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        bool positive = mode == "canary-positive-control", suppressed = mode == "canary-suppressed";
        long secondOrdinal = 0;
        if (positive)
        {
            using ManualResetEventSlim progress = new(); Thread worker = new(() => progress.Set()) { Name = "PrivateCaptureCounterDrain" }; worker.Start();
            Check.Require(progress.Wait(TimeSpan.FromSeconds(2)) && worker.Join(TimeSpan.FromSeconds(2)) && Volatile.Read(ref FlowCanaryState.DrainObserved) == 1, "Unsafe detector did not fire.");
        }
        else
        {
            var pending = candidate.InvokeAsync("owned-success", FlowCanaryState.Probe, FlowCanaryState.FakeAwaitAsync, suppressed);
            FlowCanaryState.AssertCaller(activity);
            Check.Require(Volatile.Read(ref FlowCanaryState.FirstProbe) == (suppressed ? 1 : 0), "First original store probe did not see prior flow state.");
            object[] first = await pending.ConfigureAwait(false); Check.Require((int)first[13] == 1, "First actual business path missing."); FlowCanaryState.AssertCaller(activity);
            CounterControl counters = new(candidate); // Eligible only, after genuine first business helper initialization.
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
            while (counters.Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
            long before = counters.Read("Admitted"); secondOrdinal = counters.Read("Ordinal") + 1;
            await candidate.InvokeAsync("owned-success", FlowCanaryState.Probe, FlowCanaryState.FakeAwaitAsync).ConfigureAwait(false);
            Check.Require(counters.Read("Admitted") - before == 1 && Volatile.Read(ref FlowCanaryState.ProbeChecks) > 0, "Second actual business admission missing.");
            await counters.FinishAsync("active").ConfigureAwait(false);
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 0, "Recorder worker inherited synthetic context.");
        }
        FlowCanaryState.AssertCaller(activity); SharedDependencies.AssertNoDefaultVariant();
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new {
            Cases = 1, Passed = 1, ErrorType = "none", PositiveControl = positive, AlreadySuppressedBranch = suppressed,
            DrainCanaryObserved = Volatile.Read(ref FlowCanaryState.DrainObserved) != 0, FakeAwaitChecks = Volatile.Read(ref FlowCanaryState.ProbeChecks),
            CallerCanaryUnchanged = true, CallerActivityUnchanged = true, CallerFlowUnchanged = true, WorkerProgressAndClosedReceipt = !positive,
            VariantMethodIdentityQualified = true, ActualCanaryOrActivityValuePersisted = false, SecondBusinessCaptureOrdinal = secondOrdinal,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        FlowCanaryState.Local.Value = null; Console.WriteLine(JsonSerializer.Serialize(new { cases = 1, passed = 1, errorType = "none" })); return 0;
    }
}
