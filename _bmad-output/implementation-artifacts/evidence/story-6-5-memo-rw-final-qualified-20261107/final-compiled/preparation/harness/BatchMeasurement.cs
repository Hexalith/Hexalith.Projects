namespace Story65.ImageMemo.Harness;

/// <summary>Stores one complete shared-memo cohort's raw wall, process CPU, allocation and collection counters.</summary>
internal sealed record BatchMeasurement(string Variant, string Path, string Phase, int Round, int Order,
    int Readers, int IterationsPerReader, int CompleteObservations, int ImagesPerObservation,
    long StopwatchFrequency, long ElapsedTicks, long ProcessCpuTicks, long ProcessAllocatedBytes,
    int[] GcCollectionDeltas, int MemoCountBefore, int MemoCountAfter,
    int ChargedBytesBefore, int ChargedBytesAfter, bool EntireCohortFitsOriginalBounds,
    int ReportedMemoHits, int FreshResultsWithoutReportedReuse, bool NonHitsDistinguishedFromRejections,
    WorkerMeasurement[] Workers);
