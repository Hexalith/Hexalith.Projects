namespace Story65.AdvisoryWaveProof;

using System.Diagnostics;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.PrivateCaptureCounter;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Hexalith.Projects.Server.Tests.Queries;

internal static class ScenarioRunner
{
    internal static async Task<ProofOutcome> RunAsync(ProofCase scenario, bool instrumented)
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync().ConfigureAwait(false);
        for (int n = 0; n < scenario.Projects; n++) await fixture.SeedAsync("project-" + (char)('a' + n)).ConfigureAwait(false);
        TraceLog trace = new();
        TracedReadStore store = new(fixture.Store, trace);
        TracedFolders folders = new(trace, scenario);
        TracedAuthorization authorization = new(trace, scenario);
        ProjectAuthorizationGate gate = new(new TenantAccessAuthorizer(fixture.Tenants, fixture.Clock, new TenantAccessOptions()),
            authorization, new AllowingProjectDaprPolicyEvidenceProvider(), new InMemoryProjectDetailReadModel());
        AdvisoryTestConversationDirectory conversations = new();
        AdvisoryTestLogger logger = new();
        object payload = scenario.Conversation ? new ResolveProjectFromConversationQuery("conversation")
            : new ResolveProjectFromAttachmentsQuery(Enumerable.Range(0, scenario.Projects)
                .Select(n => new ProjectAdvisoryAttachment("folder", "folder-project-" + (char)('a' + n))).ToArray());
        if (scenario.Mutation == "no-signal") payload = new ResolveProjectFromAttachmentsQuery([]);
        if (scenario.Mutation == "file-only") payload = new ResolveProjectFromAttachmentsQuery([new("file", "synthetic-file")]);
        if (scenario.Mutation == "raw-33") payload = new ResolveProjectFromAttachmentsQuery(Enumerable.Repeat(new ProjectAdvisoryAttachment("folder", "folder-project-a"), 33).ToArray());
        if (scenario.Mutation == "anchor-change" && !scenario.Conversation)
            payload = new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a"), new("folder", "new-denied-folder")]);
        if (scenario.Mutation == "conversation-no-signal") conversations.OnRead = _ => AdvisoryTestConversationDirectory.Result(new(new("conversation", null, null, "Open"), false));
        QueryEnvelope query = fixture.Query(payload) with { QueryType = scenario.Conversation
            ? ResolveProjectFromConversationQuery.QueryType : ResolveProjectFromAttachmentsQuery.QueryType };
        if (scenario.Mutation == "wrong-type") query = query with { QueryType = ResolveProjectFromConversationQuery.QueryType };
        if (scenario.Mutation == "invalid-payload") query = query with { Payload = "{\"attachments\":[],\"attachments\":[]}"u8.ToArray() };
        if (scenario.Mutation == "invalid-envelope") query = query with { AggregateId = "other" };
        string[] snapshotKeys = [ProjectReadModelKeys.Index("tenant-a"), .. Enumerable.Range(0, scenario.Projects)
            .Select(n => ProjectReadModelKeys.Detail("tenant-a", "project-" + (char)('a' + n)))];
        byte[][] before = snapshotKeys.Select(key => fixture.Store.SnapshotRaw("statestore", key) ?? []).ToArray();
        int storeCount = fixture.Store.Count;
        using CancellationTokenSource caller = new();
        if (scenario.Mutation == "pre-canceled") caller.Cancel();
        TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Dictionary<int, FourReaderBarrier> barriers = new() { [2] = new(), [4] = new(), [5] = new(), [6] = new() };
        int faulted = 0;
        TraceEntry? injected = null;
        trace.Before = async entry =>
        {
            if (scenario.Mutation == "parallel-four" && barriers.TryGetValue(entry.Wave, out var barrier)
                && entry.Operation == (entry.Wave is 2 or 5 ? "input-folder" : "project-authorize"))
                await barrier.ArriveAsync(entry.Token).ConfigureAwait(false);
            if (scenario.Mutation == "shared-deadline" && entry.Wave is 1 or 3)
                await Task.Delay(1100, entry.Token).ConfigureAwait(false);
            if (entry.Wave != scenario.FaultWave || Interlocked.CompareExchange(ref faulted, 1, 0) != 0) return;
            injected = entry;
            if (scenario.Fault == "throw") throw new FixtureFailure();
            if (scenario.Fault == "dependency-cancel") await Task.FromCanceled(new CancellationToken(true)).ConfigureAwait(false);
            if (scenario.Fault == "caller-cancel") { caller.Cancel(); entry.Token.ThrowIfCancellationRequested(); }
            if (scenario.Fault is "uncooperative" or "uncooperative-caller")
            { reached.TrySetResult(); await release.Task.ConfigureAwait(false); }
        };
        trace.After = entry =>
        {
            if (entry.Wave == 4 && entry.Operation == "anchor-folder" && scenario.Mutation is "anchor-change" or "lifecycle-change")
            {
                string key = ProjectReadModelKeys.Detail("tenant-a", entry.Item);
                var model = fixture.Store.Snapshot<ProjectReadModel>("statestore", key)!;
                fixture.Store.SeedRaw("statestore", key, model with { Detail = scenario.Mutation == "anchor-change"
                    ? model.Detail! with { ProjectFolder = model.Detail.ProjectFolder! with { FolderId = "new-denied-folder" } }
                    : model.Detail! with { Lifecycle = ProjectLifecycle.Archived } });
            }
            return Task.CompletedTask;
        };
        Exception? expectedClockError = scenario.Mutation == "clock-exception-identity" ? new FixtureFailure() : null;
        TimeProvider executorClock = expectedClockError is null ? fixture.Clock : new ThrowingTestClock(expectedClockError);
        var original = new ProjectAdvisoryQueryExecutor(store, gate, fixture.Binding, folders, conversations, executorClock, logger);
        var changed = new InstrumentedProjectAdvisoryQueryExecutor(store, gate, fixture.Binding, folders, conversations, executorClock, logger);
        QueryResult? result = null;
        Exception? error = null;
        long admittedBefore = CounterControl.Read("Admitted");
        Stopwatch elapsed = Stopwatch.StartNew();
        Task<QueryResult> pending;
        try
        {
            QueryEnvelope input = scenario.Mutation == "null-query" ? null! : query;
            pending = instrumented
                ? scenario.Conversation ? changed.FromConversationAsync(input, caller.Token) : changed.FromAttachmentsAsync(input, caller.Token)
                : scenario.Conversation ? original.FromConversationAsync(input, caller.Token) : original.FromAttachmentsAsync(input, caller.Token);
            if (scenario.Fault == "uncooperative-caller")
            { await reached.Task.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); caller.Cancel(); }
            result = await pending.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception; }
        elapsed.Stop();
        long[]? closedTerminal = instrumented ? CounterControl.TerminalValues(changed.TerminalForProof) : null;
        long[]? closedSpans = instrumented ? CounterControl.ElapsedValues(changed.TerminalForProof) : null;
        bool childAfterReturn = trace.Active != 0;
        if (scenario.Fault is "uncooperative" or "uncooperative-caller")
        {
            // WaitAsync can wake before the parallel loop's linked token callback runs. Keep the mock child held
            // until it has actually observed cancellation, so releasing it cannot race a legal consumed prefix.
            using CancellationTokenSource propagated = new(TimeSpan.FromSeconds(3));
            while (injected is not null && Volatile.Read(ref injected.CancellationObserved) == 0)
                await Task.Delay(1, propagated.Token).ConfigureAwait(false);
            TraceLog.Require(injected is not null && injected.Token.IsCancellationRequested, "Held child did not observe canceled dependency token.");
        }
        release.TrySetResult();
        await trace.DrainAsync().ConfigureAwait(false);
        if (instrumented)
        {
            long[]? afterLateChildren = CounterControl.TerminalValues(changed.TerminalForProof);
            TraceLog.Require(closedTerminal is null ? afterLateChildren is null
                : afterLateChildren is not null && closedTerminal.SequenceEqual(afterLateChildren),
                "Late callbacks changed a closed terminal record.");
            TerminalExpectations.Check(scenario, closedTerminal);
            TraceLog.Require(closedSpans is null ? CounterControl.ElapsedValues(changed.TerminalForProof) is null
                : closedSpans.SequenceEqual(CounterControl.ElapsedValues(changed.TerminalForProof)!),
                "Late callbacks changed closed boundary spans.");
            BoundarySpanExpectations.Check(scenario, closedSpans);
        }
        long admitted = CounterControl.Read("Admitted") - admittedBefore;
        trace.AssertPartialOrders();
        AssertFault(scenario, faulted, injected, trace);
        if (scenario.Mutation == "parallel-four")
            foreach (int wave in new[] { 2, 4, 5, 6 }) TraceLog.Require(trace.Maxima[wave] == 4, "Four-reader barrier was not exercised.");
        if (scenario.Fault is "uncooperative" or "uncooperative-caller") TraceLog.Require(childAfterReturn, "Child incorrectly quiesced with caller wait.");
        if (scenario.Mutation == "shared-deadline" || scenario.Fault == "uncooperative")
            TraceLog.Require(elapsed.Elapsed >= TimeSpan.FromMilliseconds(1600) && elapsed.Elapsed < TimeSpan.FromSeconds(4), "Shared two-second wait changed.");
        TraceLog.Require(fixture.Store.Count == storeCount, "Advisory wrote a read model.");
        byte[][] after = snapshotKeys.Select(key => fixture.Store.SnapshotRaw("statestore", key) ?? []).ToArray();
        if (scenario.Mutation is not ("anchor-change" or "lifecycle-change"))
            TraceLog.Require(before.Zip(after).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second)), "Advisory mutated persisted bytes.");
        AssertExpected(scenario, result, error, trace);
        if (expectedClockError is not null) TraceLog.Require(ReferenceEquals(error, expectedClockError), "Original clock exception instance was replaced.");
        return new(result, error, trace, elapsed.Elapsed, caller.IsCancellationRequested, childAfterReturn, storeCount, after,
            logger.Entries.Select(entry => JsonSerializer.Serialize(new { entry.Level, entry.Message,
                Exception = entry.Exception?.GetType().FullName, Keys = entry.Keys.Order(StringComparer.Ordinal).ToArray() }))
                .Order(StringComparer.Ordinal).ToArray(), admitted, faulted,
            error is OperationCanceledException canceled && canceled.CancellationToken == caller.Token, closedTerminal,
            expectedClockError is not null && ReferenceEquals(error, expectedClockError), closedSpans);
    }

    private static void AssertFault(ProofCase scenario, int faulted, TraceEntry? injected, TraceLog trace)
    {
        if (scenario.Fault == "none") { TraceLog.Require(faulted == 0 && injected is null, "Unexpected fault injection."); return; }
        TraceLog.Require(faulted == 1 && injected is not null && injected.Wave == scenario.FaultWave, "Requested fault did not fire exactly once at its wave.");
        if (scenario.Fault == "throw") TraceLog.Require(injected!.Error == typeof(FixtureFailure).FullName, "Requested throw was not observed.");
        if (scenario.Fault == "dependency-cancel") TraceLog.Require(injected!.Error == typeof(TaskCanceledException).FullName, "Requested canceled dependency was not observed.");
        if (scenario.Fault == "caller-cancel") TraceLog.Require(injected!.CancellationObserved == 1
            && injected.Error == typeof(OperationCanceledException).FullName, "Caller cancellation was not observed at the requested dependency.");
        Dictionary<int, string[]> healthy = new()
        {
            [1] = ["scope-authorize"], [2] = ["input-folder"], [3] = ["index"],
            [4] = ["project-authorize", "detail", "anchor-folder"], [5] = ["input-folder"],
            [6] = ["project-authorize", "detail", "anchor-folder"], [7] = ["scope-authorize"],
        };
        foreach (int wave in healthy.Keys.Where(wave => wave < scenario.FaultWave))
            TraceLog.Require(trace.Entries.Where(entry => entry.Wave == wave).Select(entry => entry.Operation).Order(StringComparer.Ordinal)
                .SequenceEqual(healthy[wave].Order(StringComparer.Ordinal)), "Injected case did not consume the required healthy prefix.");
        if (scenario.Fault == "caller-cancel" || scenario.Fault is "uncooperative" or "uncooperative-caller")
            TraceLog.Require(trace.Entries.All(entry => entry.Wave <= scenario.FaultWave), "Work advanced to a later wave after canceled caller wait.");
        if (scenario.Fault is "throw" or "dependency-cancel")
        {
            string[] allowedLater = scenario.FaultWave switch
            {
                2 => ["7:scope-authorize"],
                4 => ["5:input-folder", "7:scope-authorize"],
                5 => ["6:project-authorize", "6:detail", "7:scope-authorize"],
                6 => ["7:scope-authorize"],
                _ => [],
            };
            TraceLog.Require(trace.Entries.Where(entry => entry.Wave > scenario.FaultWave).Select(entry => entry.Wave + ":" + entry.Operation)
                .Order(StringComparer.Ordinal).SequenceEqual(allowedLater.Order(StringComparer.Ordinal)), "Consumed suffix changed after a caught failure.");
        }
    }

    private static void AssertExpected(ProofCase scenario, QueryResult? result, Exception? error, TraceLog trace)
    {
        if (scenario.Fault is "caller-cancel" or "uncooperative-caller" || scenario.Mutation == "pre-canceled")
        { TraceLog.Require(error is OperationCanceledException, "Caller cancellation did not propagate."); return; }
        if (scenario.Mutation == "null-query") { TraceLog.Require(error is ArgumentNullException, "Null guard changed."); return; }
        if (scenario.Mutation == "clock-exception-identity") { TraceLog.Require(error is FixtureFailure, "Clock exception did not propagate."); return; }
        TraceLog.Require(error is null && result is not null, "Unexpected top-level exception.");
        if (scenario.Mutation is "deny-initial" or "revoke-scope" or "wrong-type")
        { TraceLog.Require(result!.ErrorMessage == "safe-denial", "Safe denial changed."); return; }
        if (scenario.Mutation is "invalid-payload" or "invalid-envelope" or "raw-33")
        { TraceLog.Require(result!.ErrorMessage == QueryAdapterFailureReason.InvalidEnvelope, "Invalid-envelope guard changed."); return; }
        TraceLog.Require(result!.Success, "Expected successful advisory envelope.");
        var payload = result.GetPayload();
        int count = payload.GetProperty("candidates").GetArrayLength();
        if (scenario.Fault is "throw" or "dependency-cancel")
            TraceLog.Require(count == 0 && payload.GetProperty("incomplete").GetBoolean(), "Injected dependency failure did not preserve incomplete coverage.");
        if (scenario.Fault == "none" && scenario.Mutation is "none" or "parallel-four") TraceLog.Require(count == scenario.Projects, "Positive fixture did not discover every seeded candidate.");
        if (scenario.Mutation == "shared-deadline" || scenario.Fault == "uncooperative")
            TraceLog.Require(count == 0 && payload.GetProperty("incomplete").GetBoolean(), "Bounded wait failure lost incomplete coverage.");
        if (scenario.Mutation is "no-signal" or "file-only" or "deny-folder" or "conversation-no-signal")
            TraceLog.Require(count == 0 && trace.Entries.All(entry => entry.Operation is not ("index" or "detail")), "No-signal fixture scanned Projects.");
        if (scenario.Mutation is "revoke-input" or "revoke-project" or "anchor-change" or "lifecycle-change")
            TraceLog.Require(count == 0, "Final disclosure reused stale evidence.");
        if (scenario.Mutation == "anchor-change")
            TraceLog.Require(trace.Entries.Any(entry => entry.Wave == 6 && entry.Operation == "anchor-folder" && entry.Argument == "new-denied-folder"), "New final Folder anchor was not observed.");
        if (scenario.Mutation == "file-only") TraceLog.Require(payload.GetProperty("incomplete").GetBoolean(), "Unsupported file lost incomplete coverage.");
        if (scenario.Mutation == "folder-unavailable")
            TraceLog.Require(count == 0 && payload.GetProperty("incomplete").GetBoolean()
                && trace.Entries.All(entry => entry.Error == "none"), "Unavailable-without-exception control changed.");
    }
}
