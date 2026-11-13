using System.Diagnostics;
namespace Story65.Canonical.Harness;
/// <summary>Times whole fresh input capture, original clone/validation/serialization and fresh scope entry/disposal.</summary>
internal static class BatchRunner
{
    internal static async Task<BatchSample> Run(Dataset dataset, string workload, string state, string variant, int readers, int round)
    {
        IVariant owner = Variants.Create(variant);
        bool ownerClosed = false;
        try
        {
        if (state == "warm")
        {
            using var scope = owner.Enter();
            GC.KeepAlive(owner.Capture(dataset.Fresh(), workload));
        }
        int iterations = state == "warm" ? 64 : 1;
        using var ready = new CountdownEvent(readers);
        using var go = new ManualResetEventSlim();
        long allocated = GC.GetTotalAllocatedBytes(precise: true);
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        Task<CaptureSample[]>[] tasks = Enumerable.Range(0, readers).Select(worker => Task.Run(() =>
        {
            ready.Signal(); go.Wait();
            var samples = new CaptureSample[iterations];
            for (int index = 0; index < iterations; index++)
            {
                long start = Stopwatch.GetTimestamp();
                using (var scope = owner.Enter()) GC.KeepAlive(owner.Capture(dataset.Fresh(), workload));
                samples[index] = new(worker, index + 1, Stopwatch.GetTimestamp() - start);
            }
            return samples;
        })).ToArray();
        ready.Wait();
        using var process = Process.GetCurrentProcess();
        double cpu = process.TotalProcessorTime.TotalMilliseconds;
        long wall = Stopwatch.GetTimestamp(); go.Set();
        CaptureSample[][] workers = await Task.WhenAll(tasks);
        wall = Stopwatch.GetTimestamp() - wall;
        cpu = process.TotalProcessorTime.TotalMilliseconds - cpu;
        long allocationDelta = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        int dg0 = GC.CollectionCount(0)-g0, dg1 = GC.CollectionCount(1)-g1, dg2 = GC.CollectionCount(2)-g2;
        object counters = owner.Counters();
        long teardown = Stopwatch.GetTimestamp();
        owner.Dispose(); ownerClosed = true;
        teardown = Stopwatch.GetTimestamp() - teardown;
        return new(dataset.Name, workload, state, variant, readers, round, readers * iterations, wall, cpu,
            allocationDelta, dg0, dg1, dg2, workers.SelectMany(samples => samples).ToArray(), teardown, counters);
        }
        finally { if (!ownerClosed) owner.Dispose(); }
    }
}
