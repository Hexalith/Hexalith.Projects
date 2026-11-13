using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Story65.ImageMemo.Harness;

/// <summary>Releases dedicated readers together; every reader shares this host memo but creates its own scopes.</summary>
internal static class BatchRunner
{
    internal static BatchMeasurement Run(IMemoHost host, ImageInventory inventory,
        string path, string phase, int round, int order, int readers, int iterations)
    {
        Require.That(readers is 1 or 2 or 4 && iterations > 0 && !host.IsClosed, "batch-inputs");
        int countBefore = host.Count;
        int retainedBefore = host.RetainedBytes;
        using var start = new Barrier(readers + 1);
        var results = new WorkerMeasurement[readers];
        var failures = new Exception?[readers];
        var observations = Enumerable.Range(0, readers)
            .Select(_ => new ObservationMeasurement[iterations]).ToArray();
        Thread[] threads = Enumerable.Range(0, readers).Select(worker => new Thread(() =>
        {
            long began = 0;
            long allocated = 0;
            string? failureType = null;
            try
            {
                start.SignalAndWait();
                began = Stopwatch.GetTimestamp();
                allocated = GC.GetAllocatedBytesForCurrentThread();
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    observations[worker][iteration] = ObservationRunner.Run(host, inventory, path, worker, iteration);
                }
            }
            catch (Exception error)
            {
                failureType = error.GetType().FullName;
                failures[worker] = error;
            }
            long ended = Stopwatch.GetTimestamp();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            results[worker] = new WorkerMeasurement(worker, began == 0 ? 0 : ended - began,
                began == 0 ? 0 : bytes - allocated, failureType, observations[worker]);
        }) { IsBackground = true }).ToArray();
        foreach (Thread thread in threads)
        {
            thread.Start();
        }
        using Process process = Process.GetCurrentProcess();
        long allAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int[] beforeGc = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        process.Refresh();
        long cpuBefore = process.TotalProcessorTime.Ticks;
        long beganBatch = Stopwatch.GetTimestamp();
        start.SignalAndWait();
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        long elapsed = Stopwatch.GetTimestamp() - beganBatch;
        process.Refresh();
        long cpu = process.TotalProcessorTime.Ticks - cpuBefore;
        long allAllocated = GC.GetTotalAllocatedBytes(precise: true) - allAllocatedBefore;
        int[] gc = Enumerable.Range(0, 3).Select(generation => GC.CollectionCount(generation) - beforeGc[generation]).ToArray();
        Exception? workerFailure = failures.FirstOrDefault(failure => failure is not null);
        if (workerFailure is not null)
        {
            ExceptionDispatchInfo.Capture(workerFailure).Throw();
        }
        Require.That(results.All(result => result is not null && result.FailureType is null), "batch-worker-failure");
        int hits = results.Sum(result => result.Observations.Sum(observation => observation.ReportedMemoHits));
        int nonHits = readers * iterations * inventory.Images.Length - hits;
        int countAfter = host.Count;
        int retainedAfter = host.RetainedBytes;
        Require.That(countAfter <= 128 && retainedAfter <= 64 * 1024 * 1024 && !host.IsClosed, "unchanged-memo-bounds");
        if (inventory.EntireCohortFitsOriginalBounds)
        {
            Require.That(countAfter == inventory.Images.Length && retainedAfter == inventory.ChargedBytes, "fit-cohort-actual-retention");
            int expectedHits = (readers * iterations - (countBefore == 0 ? 1 : 0)) * inventory.Images.Length;
            Require.That(hits == expectedHits, "fit-cohort-actual-hit-count");
        }
        return new BatchMeasurement(host.Name, path, phase, round, order, readers, iterations,
            readers * iterations, inventory.Images.Length, Stopwatch.Frequency, elapsed, cpu,
            allAllocated, gc, countBefore, countAfter, retainedBefore, retainedAfter,
            inventory.EntireCohortFitsOriginalBounds, hits, nonHits,
            inventory.EntireCohortFitsOriginalBounds, results);
    }
}
