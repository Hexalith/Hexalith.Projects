namespace Story65.InvocationTransportProof;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
internal static class ExcludedDriver
{
    internal static async Task<int> RunAsync(string[] args, SdkVariant ordinary, SdkVariant candidate)
    {
        Check.Require(Assembly.GetEntryAssembly()?.GetName().Name == "Story65.InvocationTransport.Excluded", "Genuine excluded entry required.");
        // Never obtain the helper/bootstrap Type, static fields or readiness here. No CounterControl construction.
        long start = Stopwatch.GetTimestamp(); Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + start);
        Check.Require(await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false) == "GO", "No external window acknowledgement.");
        Check.Require(Stopwatch.GetTimestamp() < start + Stopwatch.Frequency, "Excluded GO arrived outside the fresh active window.");
        int passed = 0, entries = 0;
        foreach (var test in CaseCatalog.All)
        {
            object[] baseline = await ordinary.InvokeAsync(test.Name).ConfigureAwait(false), instrumented = await candidate.InvokeAsync(test.Name).ConfigureAwait(false);
            ResultComparison.Equal(baseline, instrumented); Check.Require((int)instrumented[13] == test.Entries, "Excluded actual entry changed."); passed++; entries += test.Entries;
        }
        Check.Require(Stopwatch.GetTimestamp() < start + 8 * Stopwatch.Frequency, "Excluded actual probes ended outside the active window.");
        // Remain a genuine live process beyond the entire external 8s window and 4s drain opportunity.
        long end = start + 12 * Stopwatch.Frequency;
        while (Stopwatch.GetTimestamp() < end) await Task.Delay(10).ConfigureAwait(false);
        SharedDependencies.AssertNoDefaultVariant();
        await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(new {
            Cases = 20, Passed = passed, ErrorType = "none", InvocationComparisons = entries, CounterStateUnavailable = true,
            HelperStateInspected = false, PrivateAdmittedCount = (int?)null, GenuineExcludedEntry = true, WindowAndClosureOpportunityLived = true,
            LivedTicks = Stopwatch.GetTimestamp() - start, Frequency = Stopwatch.Frequency, TraceValuesPersisted = false, PayloadHeaderProofValuesPersisted = false,
            VariantIdentitiesQualified = true, SharedDependenciesQualified = true,
        }, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = 20, passed, errorType = "none" })); return passed == 20 ? 0 : 1;
    }
}
