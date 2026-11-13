namespace Story65.ImageMemo.Harness;

/// <summary>Stores raw dedicated-reader observations without attributing process CPU to that thread.</summary>
internal sealed record WorkerMeasurement(int Worker, long ElapsedTicks, long ThreadAllocatedBytes,
    string? FailureType, ObservationMeasurement[] Observations);
