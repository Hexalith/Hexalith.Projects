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
var enterMemo = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!
    .GetMethod("EnterWithMemo", BindingFlags.Static | BindingFlags.NonPublic)!;
Type memoType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogValidatedBytesMemo")!;
IDisposable Enter() => (IDisposable)enter.Invoke(null, null)!;
var results = new List<object>();
foreach (string mode in new[] { "per-acquisition-scope", "actual-owned-byte-scopes", "current-release-binding", "actual-owned-byte-release-binding" })
{
    bool retained = mode.StartsWith("actual");
    bool binding = mode.Contains("binding");
    IDisposable? memo = retained ? (IDisposable)Activator.CreateInstance(memoType, nonPublic: true)! : null;
    IDisposable? Open() => retained ? (IDisposable)enterMemo.Invoke(null, [memo])! : !binding ? Enter() : null;
    try
    {
        if (retained) { using var seed = Open(); RouteCatalogCodec.Decode(bytes, root); }
        for (int warm = 0; warm < 8; warm++)
        {
            using var scope = Open();
            if (binding) RouteCatalogActivationBinding.ComputeDigest(activation);
            else for (int call = 0; call < 7; call++) RouteCatalogCodec.Decode(bytes, root);
        }
        var times = new List<double>();
        long allocated = GC.GetTotalAllocatedBytes(true);
        for (int operation = 0; operation < 40; operation++)
        {
            long started = Stopwatch.GetTimestamp();
            using var scope = Open();
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
    finally { memo?.Dispose(); }
}
if (!RouteCatalogCodec.Encode(RouteCatalogCodec.Decode(bytes, root)).AsSpan().SequenceEqual(bytes)) throw new Exception("Roundtrip differs");
Console.WriteLine(JsonSerializer.Serialize(new { catalogBytes = bytes.Length, root, canonicalBytesAndFacetsUnchanged = true,
    actualMemoImplementationMeasured = true, authorityStateHostTrustLeaseOrRuntimeCallsPerformed = false, results }, new JsonSerializerOptions { WriteIndented = true }));
