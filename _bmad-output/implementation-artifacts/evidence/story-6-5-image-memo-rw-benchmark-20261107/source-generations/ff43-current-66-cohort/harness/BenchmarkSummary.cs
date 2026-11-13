namespace Story65.ImageMemo.Harness;

/// <summary>Summarizes the raw matched samples without an automatic speedup or full-host acceptance claim.</summary>
internal static class BenchmarkSummary
{
    internal static object Build(BatchMeasurement[] rows)
    {
        return new
        {
            DiagnosticOnly = true,
            FeatureAcceptance = false,
            ProductionCandidateBenefitClaimed = false,
            Domain66IsNotWholeHostCaptureOrDeadlineBenchmark = true,
            SourceDerivedOwnerDomainDirectReferenceCohortOnly = true,
            CompleteDomainExecutableOwnersDiscoveryUnionNotClaimed = true,
            Technical66IsSeparatePeMvidStressNotClaimedActualHostCallPopulation = true,
            NoFileReadAuthorityOrConfigurationObservationRemoved = true,
            MemoColdMeansNewMemoNotColdFilesystemJitPoolOrProcess = true,
            OriginalReaderCountAndByteBoundsUnchanged = true,
            BalancedAllSixVariantPermutationsPerPathAndReaderCount = true,
            BatchStartBarrierOnlyNoPerImageBarrier = true,
            ComparisonAndMeasurementBookkeepingIncludedIdentically = true,
            RawBatches = rows.Length,
            Results = rows.GroupBy(row => new { row.Variant, row.Path, row.Readers, row.Phase })
                .Select(group => new
                {
                    group.Key.Variant,
                    group.Key.Path,
                    group.Key.Readers,
                    group.Key.Phase,
                    Samples = group.Count(),
                    BatchWallMilliseconds = Stats(group.Select(row => 1000d * row.ElapsedTicks / row.StopwatchFrequency)),
                    ProcessCpuMilliseconds = Stats(group.Select(row => row.ProcessCpuTicks / 10000d)),
                    ProcessAllocatedBytesPerObservation = Stats(group.Select(row => (double)row.ProcessAllocatedBytes / row.CompleteObservations)),
                    ObservationWallMilliseconds = Stats(group.SelectMany(row => row.Workers.SelectMany(worker => worker.Observations)
                        .Select(observation => 1000d * observation.ElapsedTicks / row.StopwatchFrequency))),
                    GcCollectionsByGeneration = Enumerable.Range(0, 3).Select(generation => group.Sum(row => row.GcCollectionDeltas[generation])).ToArray(),
                    TotalReportedMemoHits = group.Sum(row => row.ReportedMemoHits),
                    TotalFreshResultsWithoutReportedReuse = group.Sum(row => row.FreshResultsWithoutReportedReuse),
                    NonHitsDistinguishedFromRejections = group.All(row => row.NonHitsDistinguishedFromRejections),
                }).ToArray(),
        };
    }

    private static object Stats(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        Require.That(sorted.Length > 0 && sorted.All(double.IsFinite), "finite-summary-values");
        double median = sorted.Length % 2 == 0
            ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2
            : sorted[sorted.Length / 2];
        return new { Count = sorted.Length, Min = sorted[0], Median = median, P95NearestRank = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], Max = sorted[^1] };
    }
}
