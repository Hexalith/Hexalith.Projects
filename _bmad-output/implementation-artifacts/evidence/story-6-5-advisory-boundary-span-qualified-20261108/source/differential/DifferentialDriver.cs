namespace Story65.AdvisoryWaveProof;

using System.Text.Json;

internal static class DifferentialDriver
{
    internal static IEnumerable<ProofCase> Cases()
    {
        // Fast controls precede any two-second waits so the one-shot eight-second window can cover all seven aliases.
        yield return new("one-candidate");
        yield return new("two-candidates", Projects: 2);
        yield return new("folder-unavailable", Mutation: "folder-unavailable");
        yield return new("clock-exception-identity", Mutation: "clock-exception-identity");
        for (int wave = 1; wave <= 7; wave++)
            foreach (string fault in new[] { "throw", "dependency-cancel", "caller-cancel" })
                yield return new("wave-" + wave + "-" + fault, FaultWave: wave, Fault: fault);
        foreach (string mutation in new[] { "deny-initial", "wrong-type", "invalid-payload", "invalid-envelope", "raw-33", "file-only", "no-signal", "deny-folder", "revoke-scope", "revoke-project", "revoke-input", "anchor-change", "lifecycle-change", "null-query", "pre-canceled" })
            yield return new(mutation, Mutation: mutation);
        yield return new("parallel-four", Mutation: "parallel-four", Projects: 4);
        yield return new("conversation-disabled", Conversation: true);
        yield return new("conversation-no-signal", Mutation: "conversation-no-signal", Conversation: true);
        yield return new("conversation-final-anchor", Mutation: "anchor-change", Conversation: true);
        yield return new("shared-deadline", Mutation: "shared-deadline", Slow: true);
        yield return new("index-uncooperative", FaultWave: 3, Fault: "uncooperative", Slow: true);
        yield return new("filter-uncooperative", FaultWave: 2, Fault: "uncooperative", Slow: true);
        yield return new("final-anchor-uncooperative", FaultWave: 6, Fault: "uncooperative", Slow: true);
        yield return new("caller-uncooperative-child", FaultWave: 4, Fault: "uncooperative-caller", Slow: true);
    }

    internal static async Task<int> RunAsync(string[] args)
    {
        string mode = args.ElementAtOrDefault(0) ?? "disabled";
        string group = args.ElementAtOrDefault(1) ?? "all";
        string receipt = args.ElementAtOrDefault(2) ?? throw new ArgumentException("A temporary receipt path is required.");
        string? only = args.ElementAtOrDefault(3);
        if (mode is not ("disabled" or "active" or "capped" or "failing-sink")) throw new ArgumentException("Closed mode required.");
        if (group is not ("all" or "fast" or "slow")) throw new ArgumentException("Closed group required.");
        if (only is not null && !Cases().Any(test => test.Name == only)) throw new ArgumentException("Closed case name required.");
        if (mode != "disabled" && group != "fast" && only is null) throw new ArgumentException("Every active slow case requires its own process/window.");
        List<object> failures = [];
        int count = 0, passed = 0, admittedCases = 0, missedCases = 0, conversationsAdmitted = 0;
        long captures = 0;
        int faultCases = 0, faultsTriggered = 0;
        int capProbePassed = 0;
        List<object> terminals = [];
        await CounterControl.ReadyAsync(mode).ConfigureAwait(false);
        if (mode == "capped") await CounterControl.FloodForCapAsync().ConfigureAwait(false);
        foreach (ProofCase scenario in Cases().Where(test => (group == "all" || test.Slow == (group == "slow")) && (only is null || test.Name == only)))
        {
            count++;
            ProofOutcome? baseline = null, changed = null;
            try
            {
                baseline = await ScenarioRunner.RunAsync(scenario, instrumented: false).ConfigureAwait(false);
                changed = await ScenarioRunner.RunAsync(scenario, instrumented: true).ConfigureAwait(false);
                Compare(baseline, changed);
                if (changed.TerminalValues is { } terminal)
                    terminals.Add(new { Case = scenario.Name, Values = terminal, SpanTicks = changed.SpanTicks });
                if (scenario.Fault != "none") { faultCases++; faultsTriggered += changed.FaultTriggered; }
                if (changed.Admitted > 0) admittedCases++; else missedCases++;
                captures += changed.Admitted;
                if (scenario.Conversation) conversationsAdmitted += (int)changed.Admitted;
                bool excluded = scenario.Conversation || scenario.Mutation is "null-query" or "pre-canceled" or "wrong-type";
                if (mode == "disabled" || excluded)
                    TraceLog.Require(changed.Admitted == 0, "Excluded branch admitted a capture.");
                else TraceLog.Require(changed.Admitted == 1, "Eligible active case was not admitted exactly once.");
                TraceLog.Require(baseline.Admitted == 0, "Compiled original unexpectedly called the private helper.");
                passed++;
            }
            catch (Exception exception) { failures.Add(new { Case = scenario.Name, ErrorType = exception.GetType().FullName,
                AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile,
                AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine }); }
            finally { baseline?.Trace.Dispose(); changed?.Trace.Dispose(); }
        }
        if (mode == "capped") capProbePassed = 1;
        await CounterControl.FinishAsync(mode).ConfigureAwait(false);
        var output = new
        {
            PreparationSource = "temp-renamed-copy-versus-compiled-original", Mode = mode, Group = group,
            Cases = count, Passed = passed, Failures = failures, ErrorType = failures.Count == 0 ? "none" : "DifferentialFailure",
            AdmittedCases = admittedCases, NotAdmittedCases = missedCases, Captures = captures,
            ConversationsAdmitted = conversationsAdmitted, PostCapParityCases = capProbePassed,
            FaultCases = faultCases, FaultsTriggered = faultsTriggered,
            TraceValuesPersisted = false, RawTokenValuesPersisted = false,
            AllocationMode = "None", TerminalCountEncodingOnly = true, Terminals = terminals,
        };
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = count, passed, errorType = failures.Count == 0 ? "none" : "DifferentialFailure" }));
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Compare(ProofOutcome baseline, ProofOutcome changed)
    {
        TraceLog.Require(baseline.Error?.GetType() == changed.Error?.GetType(), "Top-level exception type changed.");
        if (baseline.Error is ArgumentException originalArgument && changed.Error is ArgumentException changedArgument)
            TraceLog.Require(originalArgument.ParamName == changedArgument.ParamName, "Argument error parameter changed.");
        TraceLog.Require(baseline.CallerCanceled == changed.CallerCanceled && baseline.HadChildAfterReturn == changed.HadChildAfterReturn, "Cancellation/lifetime outcome changed.");
        TraceLog.Require(baseline.FaultTriggered == changed.FaultTriggered, "Fault injection coverage changed.");
        TraceLog.Require(baseline.ErrorTokenIsCaller == changed.ErrorTokenIsCaller, "Cancellation exception lost caller-token identity.");
        TraceLog.Require(baseline.ErrorIsExpectedClock == changed.ErrorIsExpectedClock, "Injected clock exception identity changed.");
        if (baseline.Result is { } original && changed.Result is { } instrumented)
        {
            TraceLog.Require(original.Success == instrumented.Success && original.ErrorMessage == instrumented.ErrorMessage
                && original.ProjectionType == instrumented.ProjectionType, "Result envelope changed.");
            TraceLog.Require((original.PayloadBytes ?? []).AsSpan().SequenceEqual(instrumented.PayloadBytes ?? []), "Payload bytes changed.");
            TraceLog.Require(JsonSerializer.Serialize(original.Metadata) == JsonSerializer.Serialize(instrumented.Metadata), "Result metadata changed.");
        }
        else TraceLog.Require(baseline.Result is null && changed.Result is null, "Result/exception shape changed.");
        TraceLog.Require(baseline.StoreCount == changed.StoreCount && baseline.StoreBytes.Length == changed.StoreBytes.Length
            && baseline.StoreBytes.Zip(changed.StoreBytes).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second)), "Persisted fixture state changed.");
        TraceLog.Require(baseline.LogEntries.SequenceEqual(changed.LogEntries), "Existing safe logging changed.");
        static TraceEntry[] Sorted(ProofOutcome outcome) => outcome.Trace.Entries.OrderBy(e => e.Wave).ThenBy(e => e.Operation, StringComparer.Ordinal)
            .ThenBy(e => e.Item, StringComparer.Ordinal).ThenBy(e => e.Argument, StringComparer.Ordinal).ToArray();
        TraceEntry[] left = Sorted(baseline), right = Sorted(changed);
        TraceLog.Require(left.Length == right.Length, "Dependency call count changed.");
        for (int n = 0; n < left.Length; n++)
        {
            if (baseline.Error is OperationCanceledException originalCancellation && changed.Error is OperationCanceledException changedCancellation)
                TraceLog.Require((originalCancellation.CancellationToken == left[n].Token) == (changedCancellation.CancellationToken == right[n].Token), "Exception/dependency token topology changed.");
            TraceLog.Require(left[n].Wave == right[n].Wave && left[n].Operation == right[n].Operation && left[n].Item == right[n].Item
                && left[n].Argument == right[n].Argument && left[n].Error == right[n].Error
                && left[n].CanBeCanceled == right[n].CanBeCanceled && left[n].CanceledOnArrival == right[n].CanceledOnArrival
                && left[n].CancellationObserved == right[n].CancellationObserved
                && left[n].Token.IsCancellationRequested == right[n].Token.IsCancellationRequested, "Dependency input/error/cancellation observation changed.");
            // Compare each run's equality graph; raw token equality across separate linked CTS instances is meaningless.
            for (int other = 0; other < left.Length; other++)
                TraceLog.Require((left[n].Token == left[other].Token) == (right[n].Token == right[other].Token), "Per-run token topology changed.");
        }
    }
}
