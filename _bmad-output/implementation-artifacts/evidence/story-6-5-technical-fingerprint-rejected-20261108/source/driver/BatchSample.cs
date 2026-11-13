using Story65.TechnicalFingerprint.Harness;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Whole-pass batch costs with independent owner setup/teardown and cumulative numeric counters.</summary>
internal sealed record BatchSample(string Variant, string State, int Readers, int Round, int Observations,
    long WallTicks, double CpuMilliseconds, long AllocatedBytes, int Gen0, int Gen1, int Gen2,
    long OwnerInitializeTicks, long OwnerInitializeAllocatedBytes, long OwnerDisposeTicks, long OwnerDisposeAllocatedBytes,
    MemoCounters CountersBefore, MemoCounters CountersAfter, ObservationSample[] Samples);
