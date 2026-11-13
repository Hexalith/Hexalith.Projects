using System.Text.Json;

using Story65.TechnicalFingerprint.Harness;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Runs only after reviewed source/rawrefs and a separate exact compiled-runtime release.</summary>
internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        Check.That(args.Length == 3, "manifest-sha-new-output-arguments");
        string output = Path.GetFullPath(args[2]);
        Check.That(!Directory.Exists(output), "fresh-output-directory");
        Directory.CreateDirectory(output);
        try
        {
            InputPins.Verify(args[0], args[1]);
            BatchSample[] samples;
            await using (MvcCorpus corpus = MvcCorpus.Create())
            {
                Check.That(corpus.MarkedTechnicalEndpoints == corpus.EligibleTechnicalEndpoints + corpus.RefusedTechnicalEndpoints
                    && corpus.EligibleEndpoints.Count == corpus.EligibleTechnicalEndpoints && corpus.EligibleTechnicalEndpoints > 0,
                    "complete-untrimmed-marked-technical-corpus");
                Check.That(!corpus.OwnerQueryMarked && !corpus.OwnerQueryEligible, "genuine-unmarked-owner-query-not-promoted");
                Check.That(corpus.AssemblyCounts.Values.Sum(value => value.Marked) == corpus.MarkedTechnicalEndpoints
                    && corpus.AssemblyCounts.Values.Sum(value => value.Eligible) == corpus.EligibleTechnicalEndpoints
                    && corpus.AssemblyCounts.Values.Sum(value => value.Refused) == corpus.RefusedTechnicalEndpoints,
                    "complete-per-assembly-technical-counts");
                Write(output, "runtime-facts.json", RuntimeFacts.Validate());
                Write(output, "corpus-counts.json", new
                {
                    corpus.MarkedTechnicalEndpoints, corpus.EligibleTechnicalEndpoints, corpus.RefusedTechnicalEndpoints, corpus.ConstrainedEligibleEndpoints,
                    corpus.AssemblyCounts, corpus.RefusalLabels, corpus.OwnerQueryMarked, corpus.OwnerQueryEligible, corpus.ServiceFilterTypesResolved,
                    CoreQueryFingerprintOptimizationClaimed = false, WholeReceiverOrCoreInventoryClaimed = false,
                    EverySuccessfulOriginalDescribeEndpointRetained = true, DescriptorSchemaAndKeyValuesPersisted = false, NoStartOrNetworkBySourceConstruction = true, NetworkIndependentlyInstrumented = false,
                });
                ParityResult parity = await Parity.RunAsync(corpus, Path.Combine(output, "parity-fixtures"));
                Check.That(!Directory.Exists(Path.Combine(output, "parity-fixtures")), "owned-public-image-fixtures-removed");
                Write(output, "parity.json", parity);
                Check.That(parity.AllPassed && parity.Checks == parity.Passed, "all-meaningful-parity-checks");
                samples = await BenchmarkDriver.Run(corpus, output);
            }
            InputPins.Verify(args[0], args[1]);
            Write(output, "summary.json", Summary.Build(samples));
            Write(output, "closed.json", new
            {
                Passed = true, Batches = samples.Length, Observations = samples.Sum(sample => sample.Observations),
                DescriptorSchemaAndKeyValuesPersisted = false, ProductionApproved = false, HostDeadlineBenefitClaimed = false,
            });
            Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Batches = samples.Length, Observations = samples.Sum(sample => sample.Observations) }));
            return 0;
        }
        catch (Exception error)
        {
            DriverFailure? driver = error as DriverFailure;
            FixtureFailure? fixture = error as FixtureFailure;
            Write(output, "failure.json", new
            {
                ErrorType = error.GetType().FullName,
                Check = driver?.Check ?? fixture?.Check,
                File = driver?.File ?? fixture?.File,
                Line = driver?.Line ?? fixture?.Line,
            });
            Console.WriteLine(JsonSerializer.Serialize(new { Passed = false, ErrorType = error.GetType().FullName }));
            return 1;
        }
    }

    private static void Write(string output, string name, object value)
        => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + "\n");
}
