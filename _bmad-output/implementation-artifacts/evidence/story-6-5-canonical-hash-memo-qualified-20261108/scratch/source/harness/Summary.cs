using System.Diagnostics;
namespace Story65.Canonical.Harness;
/// <summary>Reports independent observation latency separately from amortized batch throughput costs.</summary>
internal static class Summary
{
    internal static object[] Build(BatchSample[] all) => all.GroupBy(sample => new {sample.Dataset,sample.Workload,sample.State,sample.Variant,sample.Readers})
        .Select(group => (object)new
        {
            group.Key, Batches=group.Count(), Observations=group.Sum(sample=>sample.Observations),
            MedianAmortizedWallMilliseconds = Median(group.Select(sample=>Ms(sample.WallTicks)/sample.Observations)),
            MedianCpuMillisecondsPerObservation=group.All(sample=>sample.CpuMilliseconds>=30)?Median(group.Select(sample=>sample.CpuMilliseconds/sample.Observations)):(double?)null,
            CpuBatchesBelow30Milliseconds=group.Count(sample=>sample.CpuMilliseconds<30),
            MedianOwnerDisposeMilliseconds=Median(group.Select(sample=>Ms(sample.OwnerDisposeTicks))),
            MedianAllocatedBytesPerObservation=Median(group.Select(sample=>(double)sample.AllocatedBytes/sample.Observations)),
            ObservationMedianMilliseconds=Median(group.SelectMany(sample=>sample.Captures).Select(sample=>Ms(sample.ElapsedTicks))),
            ObservationP95Milliseconds=Percentile(group.SelectMany(sample=>sample.Captures).Select(sample=>Ms(sample.ElapsedTicks)), .95),
        }).ToArray();
    private static double Ms(long ticks)=>1000.0*ticks/Stopwatch.Frequency;
    private static double Median(IEnumerable<double> values) { double[] x=values.Order().ToArray(); return (x[(x.Length-1)/2]+x[x.Length/2])/2; }
    private static double Percentile(IEnumerable<double> values,double p) { double[] x=values.Order().ToArray(); return x[(int)Math.Ceiling(x.Length*p)-1]; }
}
