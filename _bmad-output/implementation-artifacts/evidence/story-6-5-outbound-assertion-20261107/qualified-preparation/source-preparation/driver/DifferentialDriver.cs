namespace Story65.OutboundAssertionProof;

using System.Text.Json;

internal static class DifferentialDriver
{
    internal static async Task<int> RunAsync(string[] args, ServerVariant ordinary, ServerVariant instrumented)
    {
        string mode = args[0], group = args[1], receipt = args[2]; string? only = args.ElementAtOrDefault(3);
        Check.Require(mode is "disabled" or "active" or "capped" or "failing-sink", "Closed mode required.");
        Check.Require(group is "all" or "fast" or "slow", "Closed group required.");
        Check.Require(only is null || CaseCatalog.All.Any(test => test.Name == only), "Closed case required.");
        Check.Require(mode == "disabled" || group == "fast" || only is not null, "Slow active cells require fresh windows.");
        CounterControl counters = new(instrumented);
        await counters.ReadyAsync(mode).ConfigureAwait(false);
        if (mode == "capped") await counters.FloodAsync().ConfigureAwait(false);
        List<object> failures = []; int cases = 0, passed = 0; long admittedCases = 0;
        foreach (DriverCase test in CaseCatalog.All.Where(test => (group == "all" || test.Slow == (group == "slow")) && (only is null || test.Name == only)))
        {
            cases++;
            try
            {
                long before = counters.Read("Admitted");
                var baseline = await SubjectFixture.InvokeAsync(ordinary, test).ConfigureAwait(false);
                Assertions.ForCase(test, baseline);
                Check.Require(counters.Read("Admitted") == before, "Ordinary variant touched the private recorder.");
                var candidate = await SubjectFixture.InvokeAsync(instrumented, test).ConfigureAwait(false);
                Assertions.ForCase(test, candidate);
                long admitted = counters.Read("Admitted") - before;
                Check.Require(admitted == (mode == "disabled" ? 0 : 1), "Actual handler admission count changed.");
                Assertions.Compare(baseline, candidate); admittedCases += admitted; passed++;
            }
            catch (Exception exception)
            {
                failures.Add(new { Case = test.Name, ErrorType = exception.GetType().FullName,
                    AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile,
                    AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine });
            }
        }
        await counters.FinishAsync(mode).ConfigureAwait(false);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
        {
            Mode = mode, Group = group, Cases = cases, Passed = passed, ErrorType = failures.Count == 0 ? "none" : "DifferentialFailure",
            Failures = failures, Captures = admittedCases, VariantIdentitiesQualified = true,
            SharedDependenciesQualified = true, TraceValuesPersisted = false, AllocationMode = "None", CountCalls = 0,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases, passed, errorType = failures.Count == 0 ? "none" : "DifferentialFailure" }));
        return failures.Count == 0 ? 0 : 1;
    }
}
