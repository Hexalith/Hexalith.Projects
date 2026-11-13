namespace Story65.DaprInvokerProof;

using System.Diagnostics;
using System.Text.Json;

internal static class FreshFlowCanary
{
    internal static async Task<int> RunAsync(string[] args, GatewayVariant instrumented)
    {
        string mode = args[0], receipt = args[2];
        Check.Require(mode is "canary-positive-control" or "canary-current" or "canary-suppressed", "Closed canary mode required.");
        using Activity activity = new("synthetic-dapr-invoker-canary"); activity.Start();
        FlowCanaryState.Local.Value = FlowCanaryState.Marker; FlowCanaryState.AssertCaller(activity, false);
        // The private helper has not been reflected/initialized. The actual invoker is its first business entry.
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        bool positive = mode == "canary-positive-control", suppressed = mode == "canary-suppressed";
        if (positive)
        {
            using ManualResetEventSlim progress = new();
            Thread worker = new(() => progress.Set()) { Name = "PrivateCaptureCounterDrain" };
            worker.Start();
            Check.Require(progress.Wait(TimeSpan.FromSeconds(2)) && worker.Join(TimeSpan.FromSeconds(2)), "Unsafe positive worker did not progress.");
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 1, "Unsafe positive detector did not fire.");
        }
        else
        {
            // Actual Acquire is the first original await; its synchronous entry observes prior flow state.
            DriverCase test = new("first-business-canary");
            var pending = SubjectFixture.InvokeAsync(instrumented, test, canary: true, suppressEntry: suppressed);
            FlowCanaryState.AssertCaller(activity, false);
            Check.Require(Volatile.Read(ref FlowCanaryState.FirstProbe) == (suppressed ? 1 : 0), "Original authority did not see restored/prior flow state.");
            var first = await pending.ConfigureAwait(false); Assertions.ForCase(test, first);
            FlowCanaryState.AssertCaller(activity, false);
            CounterControl counters = new(instrumented); // Reflection begins only after actual first business initialization.
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            while (counters.Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
            long before = counters.Read("Admitted");
            var second = await SubjectFixture.InvokeAsync(instrumented, test, canary: true).ConfigureAwait(false);
            Assertions.ForCase(test, second);
            Check.Require(counters.Read("Admitted") - before == 1, "Private Gateway worker did not admit the second actual invoker.");
            Check.Require(Volatile.Read(ref FlowCanaryState.ProbeChecks) >= 8, "Authority/HTTP/buffering/release fake awaits were not exercised.");
            await counters.FinishAsync("active").ConfigureAwait(false);
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 0, "Gateway recorder thread inherited synthetic context.");
        }
        FlowCanaryState.AssertCaller(activity, false);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
        {
            Cases = 1, Passed = 1, ErrorType = "none", PositiveControl = positive, AlreadySuppressedBranch = suppressed,
            DrainCanaryObserved = Volatile.Read(ref FlowCanaryState.DrainObserved) != 0,
            FakeAwaitChecks = Volatile.Read(ref FlowCanaryState.ProbeChecks), CallerCanaryUnchanged = true,
            CallerActivityUnchanged = true, CallerFlowUnchanged = true, WorkerProgressAndClosedReceipt = !positive,
            VariantMethodIdentityQualified = true, ActualCanaryOrActivityValuePersisted = false,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = 1, passed = 1, errorType = "none" }));
        FlowCanaryState.Local.Value = null; return 0;
    }
}
