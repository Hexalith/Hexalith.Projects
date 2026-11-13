using System.Diagnostics;

namespace Story65.ImageMemo.Harness;

/// <summary>Calls only the selected adapter for every real image under a new scope in each observation.</summary>
internal static class ObservationRunner
{
    internal static ObservationMeasurement Run(IMemoHost host, ImageInventory inventory,
        string path, int worker, int iteration)
    {
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        int hits;
        using (IObservation observation = host.BeginObservation())
        {
            Require.That(observation.ScopeType == host.ScopeType, "observation-selected-scope");
            for (int index = 0; index < inventory.Images.Length; index++)
            {
                string actual = path switch
                {
                    "domain" => host.ComputeDomain(inventory.Images[index]),
                    "technical" => host.ComputeTechnical(inventory.Images[index]),
                    _ => throw new InvalidOperationException("observation-closed-path"),
                };
                string expected = path == "domain" ? inventory.Files[index].Sha256 : inventory.Files[index].ExpectedTechnicalIdentity;
                Require.That(string.Equals(actual, expected, StringComparison.Ordinal), "complete-fresh-identity-parity");
            }
            hits = observation.CacheHits;
            Require.That(hits >= 0 && hits <= inventory.Images.Length, "observation-hit-range");
        }
        long elapsed = Stopwatch.GetTimestamp() - started;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return new ObservationMeasurement(worker, iteration, elapsed, allocated, hits, inventory.Images.Length - hits);
    }
}
