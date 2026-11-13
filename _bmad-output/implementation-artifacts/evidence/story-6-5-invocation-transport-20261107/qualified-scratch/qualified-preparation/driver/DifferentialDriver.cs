namespace Story65.InvocationTransportProof;
using System.Text.Json;
internal static class DifferentialDriver
{
    internal static async Task<int> RunAsync(string[] args, SdkVariant ordinary, SdkVariant candidate)
    {
        string mode = args[0], group = args[1], receipt = args[2]; string? only = args.ElementAtOrDefault(3);
        Check.Require(mode is "disabled" or "active" or "capped" or "failing-sink", "Closed mode required.");
        Check.Require(group is "fast" or "slow", "Closed group required.");
        Check.Require(only is null || CaseCatalog.All.Any(t => t.Name == only), "Closed case required.");
        Check.Require(group == "fast" || only is not null, "Slow cells require separate windows.");
        CounterControl counters = new(candidate); await counters.ReadyAsync(mode).ConfigureAwait(false);
        if (mode == "capped") await counters.FloodAsync().ConfigureAwait(false);
        List<object> failures = [], ranges = []; int cases = 0, passed = 0, comparisons = 0; long captures = 0;
        foreach (var test in CaseCatalog.All.Where(t => t.Slow == (group == "slow") && (only is null || t.Name == only)))
        {
            cases++;
            try
            {
                long before = counters.Read("Admitted"), ordinal = counters.Read("Ordinal");
                object[] baseline = await ordinary.InvokeAsync(test.Name).ConfigureAwait(false);
                Check.Require(counters.Read("Admitted") == before && counters.Read("Ordinal") == ordinal, "Ordinary touched private recorder.");
                object[] instrumented = await candidate.InvokeAsync(test.Name).ConfigureAwait(false);
                ResultComparison.Equal(baseline, instrumented);
                Check.Require((int)baseline[13] == test.Entries && (int)instrumented[13] == test.Entries, "Actual recursive entry count differs.");
                long admitted = counters.Read("Admitted") - before;
                Check.Require(admitted == (mode == "disabled" ? 0 : test.Entries) && counters.Read("Ordinal") - ordinal == admitted, "Actual SDK admission count differs.");
                ranges.Add(new { Case = test.Name, FirstOrdinal = ordinal + 1, Entries = admitted, ExpectedStages = test.ExpectedStages });
                captures += admitted; comparisons += test.Entries; passed++;
            }
            catch (Exception exception) { SafeFailure safe = SafeFailure.From(exception); failures.Add(new { Case = test.Name, ErrorType = safe.Kind, safe.AssertionFile, safe.AssertionLine }); }
        }
        await counters.FinishAsync(mode).ConfigureAwait(false);
        SharedDependencies.AssertNoDefaultVariant();
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new {
            Mode = mode, Group = group, Cases = cases, Passed = passed, ErrorType = failures.Count == 0 ? "none" : "DifferentialFailure", Failures = failures,
            Captures = captures, InvocationComparisons = comparisons, StageExpectations = ranges, VariantIdentitiesQualified = true, SharedDependenciesQualified = true,
            TraceValuesPersisted = false, PayloadHeaderProofValuesPersisted = false, AllocationMode = "None", CountCalls = 0,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases, passed, errorType = failures.Count == 0 ? "none" : "DifferentialFailure" }));
        return failures.Count == 0 ? 0 : 1;
    }
}
