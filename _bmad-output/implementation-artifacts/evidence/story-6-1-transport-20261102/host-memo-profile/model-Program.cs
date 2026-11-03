using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

byte[] bytes = File.ReadAllBytes(args[0]);
string root = Convert.ToHexStringLower(SHA256.HashData(bytes));
var activation = JsonSerializer.Deserialize<RouteCatalogActivation>(File.ReadAllBytes(args[1]), JsonSerializerOptions.Web)!;
if (root != activation.RootDigest || !bytes.AsSpan().SequenceEqual(activation.CanonicalCatalog)) throw new Exception("Input binding differs");
var expected = RouteCatalogCodec.Decode(bytes, root);
var enter = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!
    .GetMethod("Enter", BindingFlags.Static | BindingFlags.NonPublic)!;
IDisposable Enter() => (IDisposable)enter.Invoke(null, null)!;
var results = new List<object>();
foreach (string mode in new[] { "per-acquisition-scope", "retained-owned-byte-model", "current-release-binding", "retained-byte-release-binding-model" })
{
    bool retained = mode.StartsWith("retained");
    bool binding = mode.Contains("binding");
    IDisposable? shared = retained ? Enter() : null;
    try
    {
        if (retained) RouteCatalogCodec.Decode(bytes, root);
        for (int warm = 0; warm < 8; warm++)
        {
            using var scope = !retained && !binding ? Enter() : null;
            if (binding) RouteCatalogActivationBinding.ComputeDigest(activation);
            else for (int call = 0; call < 7; call++) RouteCatalogCodec.Decode(bytes, root);
        }
        var times = new List<double>();
        long allocated = GC.GetTotalAllocatedBytes(true);
        for (int operation = 0; operation < 40; operation++)
        {
            long started = Stopwatch.GetTimestamp();
            using var scope = !retained && !binding ? Enter() : null;
            if (binding) _ = RouteCatalogActivationBinding.ComputeDigest(activation);
            else for (int call = 0; call < 7; call++)
            {
                var decoded = RouteCatalogCodec.Decode(bytes, root);
                if (decoded.RouteFacetDigest != expected.RouteFacetDigest || decoded.IdempotencyFacetDigest != expected.IdempotencyFacetDigest)
                    throw new Exception("Decoded facets differ");
            }
            times.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        allocated = GC.GetTotalAllocatedBytes(true) - allocated;
        times.Sort();
        results.Add(new { mode, operations = 40, medianMilliseconds = times[20], p95Milliseconds = times[38], averageAllocatedBytes = allocated / 40 });
    }
    finally { shared?.Dispose(); }
}
if (!RouteCatalogCodec.Encode(RouteCatalogCodec.Decode(bytes, root)).AsSpan().SequenceEqual(bytes)) throw new Exception("Roundtrip differs");
Console.WriteLine(JsonSerializer.Serialize(new { catalogBytes = bytes.Length, root, canonicalBytesAndFacetsUnchanged = true,
    modelOnly = true, authorityStateHostTrustLeaseOrRuntimeCallsPerformed = false, results }, new JsonSerializerOptions { WriteIndented = true }));
