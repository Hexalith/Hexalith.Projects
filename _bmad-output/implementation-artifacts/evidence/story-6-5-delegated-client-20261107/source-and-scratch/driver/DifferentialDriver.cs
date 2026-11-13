namespace Story65.DelegatedClientProof;

using System.Text.Json;

internal static class DifferentialDriver
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string mode = args[0], group = args[1], receipt = args[2]; string? only = args.ElementAtOrDefault(3);
        Check.Require(mode is "disabled" or "active" or "capped" or "failing-sink", "Closed mode required.");
        Check.Require(group is "all" or "fast" or "slow", "Closed group required.");
        Check.Require(only is null || CaseCatalog.All.Any(test => test.Name == only), "Closed case name required.");
        Check.Require(mode == "disabled" || group == "fast" || only is not null, "Slow active cases need individual windows.");
        SharedSyntheticKey.Prepare(); // Before READY and the window, never a production key.
        await CounterControl.ReadyAsync(mode).ConfigureAwait(false);
        if (mode == "capped") await CounterControl.FloodForCapAsync().ConfigureAwait(false);
        List<object> failures = []; int cases = 0, passed = 0, invocationComparisons = 0, eligibleAdmitted = 0;
        int faultCases = 0, faultCallbacks = 0; long captures = 0;
        foreach (DriverCase test in CaseCatalog.All.Where(test => (group == "all" || test.Slow == (group == "slow")) && (only is null || test.Name == only)))
        {
            cases++;
            try
            {
                long beforeOriginal = CounterControl.Read("Admitted");
                CohortBarrier? originalCohort = test.Concurrency > 1 ? new(test.Concurrency) : null;
                var baseline = await Task.WhenAll(Enumerable.Range(0, test.Concurrency)
                    .Select(_ => SubjectFixture.InvokeAsync(test, false, cohort: originalCohort))).ConfigureAwait(false);
                Check.Require(CounterControl.Read("Admitted") == beforeOriginal, "Compiled oracle admitted a diagnostic capture.");
                long beforeCandidate = CounterControl.Read("Admitted");
                CohortBarrier? candidateCohort = test.Concurrency > 1 ? new(test.Concurrency) : null;
                var candidate = await Task.WhenAll(Enumerable.Range(0, test.Concurrency)
                    .Select(_ => SubjectFixture.InvokeAsync(test, true, cohort: candidateCohort))).ConfigureAwait(false);
                long admitted = CounterControl.Read("Admitted") - beforeCandidate;
                int expected = mode != "disabled" && CaseCatalog.Eligible(test) ? test.Concurrency : 0;
                Check.Require(admitted == expected, "Actual selected/excluded admission count changed.");
                for (int index = 0; index < baseline.Length; index++) Assertions.Compare(baseline[index], candidate[index]);
                invocationComparisons += test.Concurrency; captures += admitted; if (expected != 0) eligibleAdmitted++;
                if (candidate.Any(item => item.State.Triggered != 0)) { faultCases++; faultCallbacks += candidate.Sum(item => item.State.Triggered); }
                passed++;
            }
            catch (Exception exception)
            {
                failures.Add(new { Case = test.Name, ErrorType = exception.GetType().FullName,
                    AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile,
                    AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine });
            }
        }
        await CounterControl.FinishAsync(mode).ConfigureAwait(false);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
        {
            Mode = mode, Group = group, Cases = cases, Passed = passed, ErrorType = failures.Count == 0 ? "none" : "DifferentialFailure",
            Failures = failures, InvocationComparisons = invocationComparisons, EligibleAdmittedCases = eligibleAdmitted,
            Captures = captures, FaultCases = faultCases, FaultCallbacks = faultCallbacks,
            TraceValuesPersisted = false, ProofHeaderKeyPayloadPersisted = false, AllocationMode = "None", CountCalls = 0,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases, passed, errorType = failures.Count == 0 ? "none" : "DifferentialFailure" }));
        return failures.Count == 0 ? 0 : 1;
    }
}
