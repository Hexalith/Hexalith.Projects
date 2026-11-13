namespace Story65.Canonical.Harness;
/// <summary>One shared-owner batch; batch wall divided by observations is amortized throughput cost.</summary>
internal sealed record BatchSample(string Dataset, string Workload, string State, string Variant, int Readers, int Round,
    int Observations, long WallTicks, double CpuMilliseconds, long AllocatedBytes, int Gen0, int Gen1, int Gen2,
    CaptureSample[] Captures, long OwnerInitializeTicks, long OwnerDisposeTicks, object Counters);
