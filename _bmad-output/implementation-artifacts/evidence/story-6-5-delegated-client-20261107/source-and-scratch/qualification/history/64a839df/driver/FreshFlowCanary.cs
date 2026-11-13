namespace Story65.DelegatedClientProof;

using System.Diagnostics;
using System.Text.Json;

internal static class FreshFlowCanary
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string mode = args[0], receipt = args[2];
        Check.Require(mode is "canary-positive-control" or "canary-current" or "canary-suppressed", "Closed canary mode required.");
        SharedSyntheticKey.Prepare();
        using Activity activity = new("synthetic-delegated-client-canary"); activity.Start();
        FlowCanaryState.Local.Value = FlowCanaryState.Marker;
        FlowCanaryState.AssertCaller(activity, false);
        // No CounterControl Ready/Read, helper reflection or helper static call precedes actual first client entry.
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        bool positive = mode == "canary-positive-control", suppressed = mode == "canary-suppressed";
        if (positive)
        {
            using ManualResetEventSlim progress = new();
            Thread worker = new(() => progress.Set()) { Name = "PrivateCaptureCounterDrain" };
            worker.Start();
            Check.Require(progress.Wait(TimeSpan.FromSeconds(2)) && worker.Join(TimeSpan.FromSeconds(2)), "Positive worker did not progress.");
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 1, "Unsafe Thread.Start detector did not fire.");
        }
        else
        {
            DriverCase test = new("first-business-canary", Budget: "00:00:03");
            var pending = SubjectFixture.InvokeAsync(test, true, canary: true, suppressFirst: suppressed);
            FlowCanaryState.AssertCaller(activity, false);
            Check.Require(Volatile.Read(ref FlowCanaryState.FirstProbe) == (suppressed ? 1 : 0), "Original issuer did not see restored/prior flow state synchronously.");
            var first = await pending.ConfigureAwait(false); Check.Require(first.Result?.Success == true, "First actual client call failed.");
            FlowCanaryState.AssertCaller(activity, false);
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            while (CounterControl.Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
            long admitted = CounterControl.Read("Admitted");
            var second = await SubjectFixture.InvokeAsync(test, true, canary: true).ConfigureAwait(false);
            Check.Require(second.Result?.Success == true && CounterControl.Read("Admitted") - admitted == 1, "Real worker window did not admit second actual client call.");
            Check.Require(Volatile.Read(ref FlowCanaryState.ProbeChecks) >= 6, "Issuer/HTTP/content fake awaits were not exercised.");
            await CounterControl.FinishAsync("active").ConfigureAwait(false);
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 0, "Recorder worker inherited the synthetic context.");
        }
        FlowCanaryState.AssertCaller(activity, false);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
        {
            Cases = 1, Passed = 1, ErrorType = "none", PositiveControl = positive, AlreadySuppressedBranch = suppressed,
            DrainCanaryObserved = Volatile.Read(ref FlowCanaryState.DrainObserved) != 0,
            FakeAwaitChecks = Volatile.Read(ref FlowCanaryState.ProbeChecks), CallerCanaryUnchanged = true,
            CallerActivityUnchanged = true, CallerFlowUnchanged = true, WorkerProgressAndClosedReceipt = !positive,
            ActualCanaryActivityOrHeaderValuePersisted = false,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = 1, passed = 1, errorType = "none" }));
        FlowCanaryState.Local.Value = null;
        return 0;
    }
}
