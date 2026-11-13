using System.Diagnostics;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Separates per-observation wall distributions from amortized batch throughput and process CPU.</summary>
internal static class Summary
{
    internal static object[] Build(BatchSample[] samples) => samples.GroupBy(value => new { value.Variant, value.State, value.Readers })
        .Select(group => (object)new
        {
            group.Key, Batches = group.Count(), Observations = group.Sum(sample => sample.Observations),
            MedianAmortizedWallMilliseconds = Median(group.Select(sample => Milliseconds(sample.WallTicks) / sample.Observations)),
            ObservationMedianMilliseconds = Median(group.SelectMany(sample => sample.Samples).Select(sample => Milliseconds(sample.ElapsedTicks))),
            ObservationP95Milliseconds = P95(group.SelectMany(sample => sample.Samples).Select(sample => Milliseconds(sample.ElapsedTicks))),
            MedianAllocatedBytesPerObservation = Median(group.Select(sample => (double)sample.AllocatedBytes / sample.Observations)),
            MedianCpuMillisecondsPerObservation = group.All(sample => sample.CpuMilliseconds >= 30)
                ? Median(group.Select(sample => sample.CpuMilliseconds / sample.Observations)) : (double?)null,
            CpuBatchesBelow30Milliseconds = group.Count(sample => sample.CpuMilliseconds < 30),
            MedianOwnerInitializeMilliseconds = Median(group.Select(sample => Milliseconds(sample.OwnerInitializeTicks))),
            MedianOwnerDisposeMilliseconds = Median(group.Select(sample => Milliseconds(sample.OwnerDisposeTicks))),
        }).ToArray();

    private static double Milliseconds(long ticks) => 1000.0 * ticks / Stopwatch.Frequency;
    private static double Median(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        return (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2;
    }
    private static double P95(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        return sorted[(int)Math.Ceiling(sorted.Length * .95) - 1];
    }
}
