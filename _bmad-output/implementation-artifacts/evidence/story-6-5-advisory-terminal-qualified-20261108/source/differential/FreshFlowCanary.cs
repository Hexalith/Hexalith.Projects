namespace Story65.AdvisoryWaveProof;

using System.Diagnostics;
using System.Text.Json;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Queries;
using Hexalith.Projects.Server.Tests.Queries;

internal static class FreshFlowCanary
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string mode = args[0], receipt = args[2];
        TraceLog.Require(mode is "canary-current" or "canary-suppressed" or "canary-positive-control", "Closed canary mode required.");
        // Do not touch CounterControl, CaptureCounters, its reflection metadata or Ready before the first business call.
        var fixture = await SupportedProjectReadFixture.CreateAsync().ConfigureAwait(false);
        await fixture.SeedAsync("project-a").ConfigureAwait(false);
        using TraceLog trace = new() { Before = FlowCanaryState.ProbeAsync };
        ProofCase scenario = new("fresh-business-canary");
        ProjectAuthorizationGate gate = new(new TenantAccessAuthorizer(fixture.Tenants, fixture.Clock, new TenantAccessOptions()),
            new TracedAuthorization(trace, scenario), new AllowingProjectDaprPolicyEvidenceProvider(), new InMemoryProjectDetailReadModel());
        var executor = new InstrumentedProjectAdvisoryQueryExecutor(new TracedReadStore(fixture.Store, trace), gate,
            fixture.Binding, new TracedFolders(trace, scenario), new AdvisoryTestConversationDirectory(), fixture.Clock);
        var query = fixture.Query(new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]))
            with { QueryType = ResolveProjectFromAttachmentsQuery.QueryType };
        using Activity activity = new("synthetic-advisory-wave-canary");
        activity.Start();
        FlowCanaryState.Local.Value = FlowCanaryState.Marker;
        FlowCanaryState.AssertCaller(activity, false);
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        bool positive = mode == "canary-positive-control";
        bool suppressed = mode == "canary-suppressed";
        if (positive)
        {
            using ManualResetEventSlim progress = new();
            Thread control = new(() => progress.Set()) { Name = "PrivateCaptureCounterDrain" };
            control.Start(); // Deliberate unsafe positive control, in its own process; no recorder is initialized.
            TraceLog.Require(progress.Wait(TimeSpan.FromSeconds(2)) && control.Join(TimeSpan.FromSeconds(2)), "Positive-control worker did not progress.");
            TraceLog.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 1, "Canary detector missed direct Thread.Start flow.");
            FlowCanaryState.AssertCaller(activity, false);
        }
        else
        {
            Task<Hexalith.EventStore.Contracts.Queries.QueryResult> first;
            if (suppressed)
            {
                using (ExecutionContext.SuppressFlow())
                {
                    FlowCanaryState.AssertCaller(activity, true);
                    first = executor.FromAttachmentsAsync(query, CancellationToken.None);
                    // Synchronous assertion before awaiting or undoing the caller-owned suppression.
                    FlowCanaryState.AssertCaller(activity, true);
                    TraceLog.Require(Volatile.Read(ref FlowCanaryState.FirstProbe) == 1, "Bootstrap undid the caller's pre-existing suppression.");
                }
            }
            else
            {
                first = executor.FromAttachmentsAsync(query, CancellationToken.None);
                FlowCanaryState.AssertCaller(activity, false);
                TraceLog.Require(Volatile.Read(ref FlowCanaryState.FirstProbe) == 0, "Bootstrap flow was not restored before original authority work.");
            }
            var firstResult = await first.ConfigureAwait(false);
            FlowCanaryState.AssertCaller(activity, false);
            TraceLog.Require(firstResult.Success && firstResult.GetPayload().GetProperty("candidates").GetArrayLength() == 1,
                "First business execution did not complete its actual dependency sequence.");
            // The helper has now first initialized through the business bootstrap. Reflection is allowed only after this point.
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            while (CounterControl.Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
            long before = CounterControl.Read("Admitted");
            // Fresh dependency ordinals are necessary: a second actual call is independently initial/final, not a third pass.
            using TraceLog secondTrace = new() { Before = FlowCanaryState.ProbeAsync };
            ProjectAuthorizationGate secondGate = new(new TenantAccessAuthorizer(fixture.Tenants, fixture.Clock, new TenantAccessOptions()),
                new TracedAuthorization(secondTrace, scenario), new AllowingProjectDaprPolicyEvidenceProvider(), new InMemoryProjectDetailReadModel());
            var second = new InstrumentedProjectAdvisoryQueryExecutor(new TracedReadStore(fixture.Store, secondTrace), secondGate,
                fixture.Binding, new TracedFolders(secondTrace, scenario), new AdvisoryTestConversationDirectory(), fixture.Clock);
            var secondResult = await second.FromAttachmentsAsync(query, CancellationToken.None).ConfigureAwait(false);
            TraceLog.Require(CounterControl.Read("Admitted") - before == 1, "Real worker window did not admit the second actual business call.");
            TraceLog.Require(secondResult.Success && secondResult.GetPayload().GetProperty("candidates").GetArrayLength() == 1,
                "Second business execution did not complete under restored caller flow.");
            secondTrace.AssertPartialOrders();
            trace.AssertPartialOrders();
            FlowCanaryState.AssertCaller(activity, false);
            TraceLog.Require(Volatile.Read(ref FlowCanaryState.ProbeChecks) >= 20, "Fake awaited dependencies were not exercised.");
            await CounterControl.FinishAsync("active").ConfigureAwait(false);
            FlowCanaryState.AssertCaller(activity, false);
            TraceLog.Require(Volatile.Read(ref FlowCanaryState.DrainObserved) == 0, "Private recorder worker inherited the synthetic canary.");
        }
        var result = new
        {
            Cases = 1, Passed = 1, ErrorType = "none", PositiveControl = positive,
            DrainCanaryObserved = Volatile.Read(ref FlowCanaryState.DrainObserved) != 0,
            CallerCanaryUnchanged = true, CallerActivityUnchanged = true, CallerFlowUnchanged = true,
            FakeAwaitChecks = Volatile.Read(ref FlowCanaryState.ProbeChecks),
            AlreadySuppressedBranch = suppressed, WorkerProgressAndClosedReceipt = !positive,
            ActualCanaryOrActivityValuePersisted = false,
        };
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = 1, passed = 1, errorType = "none" }));
        FlowCanaryState.Local.Value = null;
        return 0;
    }
}
