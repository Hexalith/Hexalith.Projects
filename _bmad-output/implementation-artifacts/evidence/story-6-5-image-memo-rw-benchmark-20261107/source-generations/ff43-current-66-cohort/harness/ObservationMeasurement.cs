namespace Story65.ImageMemo.Harness;

/// <summary>Stores one complete original-order observation, including selected scope disposal.</summary>
internal readonly record struct ObservationMeasurement(int Worker, int Iteration, long ElapsedTicks,
    long ThreadAllocatedBytes, int ReportedMemoHits, int FreshResultsWithoutReportedReuse);
