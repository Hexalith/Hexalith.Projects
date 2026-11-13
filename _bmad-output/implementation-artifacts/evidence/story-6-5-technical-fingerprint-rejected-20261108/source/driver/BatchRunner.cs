using System.Diagnostics;

using Story65.TechnicalFingerprint.Harness;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Shares one variant owner among readers with distinct measured observation scopes.</summary>
internal static class BatchRunner
{
    internal const int WarmIterations = 32;

    internal static async Task<BatchSample> Run(MvcCorpus corpus, string variant, string state, int readers, int round)
    {
        long ownerAllocated = GC.GetTotalAllocatedBytes(precise: true);
        long initialize = Stopwatch.GetTimestamp();
        ITechnicalHost owner = VariantHosts.Create(variant);
        initialize = Stopwatch.GetTimestamp() - initialize;
        ownerAllocated = GC.GetTotalAllocatedBytes(precise: true) - ownerAllocated;
        bool closed = false;
        try
        {
            if (state == "warm") Observe(owner, corpus, 0, 0);
            MemoCounters before = owner.Counters;
            int iterations = state == "warm" ? WarmIterations : 1;
            using var ready = new CountdownEvent(readers);
            using var go = new ManualResetEventSlim();
            long allocated = GC.GetTotalAllocatedBytes(precise: true);
            int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
            Task<ObservationSample[]>[] tasks = Enumerable.Range(0, readers).Select(reader => Task.Run(() =>
            {
                ready.Signal(); go.Wait();
                var samples = new ObservationSample[iterations];
                for (int index = 0; index < iterations; index++) samples[index] = Observe(owner, corpus, reader, index + 1);
                return samples;
            })).ToArray();
            ready.Wait();
            using var process = Process.GetCurrentProcess();
            double cpu = process.TotalProcessorTime.TotalMilliseconds;
            long wall = Stopwatch.GetTimestamp(); go.Set();
            ObservationSample[][] samples = await Task.WhenAll(tasks);
            wall = Stopwatch.GetTimestamp() - wall;
            cpu = process.TotalProcessorTime.TotalMilliseconds - cpu;
            long allocationDelta = GC.GetTotalAllocatedBytes(precise: true) - allocated;
            int dg0 = GC.CollectionCount(0) - g0, dg1 = GC.CollectionCount(1) - g1, dg2 = GC.CollectionCount(2) - g2;
            MemoCounters after = owner.Counters;
            Check.That(after.Entries >= 0 && after.Entries <= 256 && after.ChargedBytes >= 0 && after.ChargedBytes <= 2 * 1024 * 1024, "one-owner-request-response-bounds");
            long disposeAllocation = GC.GetTotalAllocatedBytes(precise: true);
            long dispose = Stopwatch.GetTimestamp();
            owner.Dispose(); closed = true;
            dispose = Stopwatch.GetTimestamp() - dispose;
            disposeAllocation = GC.GetTotalAllocatedBytes(precise: true) - disposeAllocation;
            return new(variant, state, readers, round, readers * iterations, wall, cpu, allocationDelta, dg0, dg1, dg2,
                initialize, ownerAllocated, dispose, disposeAllocation, before, after, samples.SelectMany(value => value).ToArray());
        }
        finally
        {
            if (!closed) owner.Dispose();
        }
    }

    private static ObservationSample Observe(ITechnicalHost owner, MvcCorpus corpus, int reader, int ordinal)
    {
        long start = Stopwatch.GetTimestamp();
        int imageHits;
        int count;
        using (IObservation scope = owner.BeginObservation())
        {
            var descriptors = owner.Observe(corpus);
            count = descriptors.Length;
            Check.That(count == corpus.EligibleTechnicalEndpoints, "complete-eligible-endpoint-pass");
            imageHits = scope.ImageHits;
            GC.KeepAlive(descriptors);
        }
        return new(reader, ordinal, Stopwatch.GetTimestamp() - start, count, imageHits);
    }
}
