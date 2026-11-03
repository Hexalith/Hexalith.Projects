using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

byte[] input = File.ReadAllBytes(args[0]);
JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
RouteCatalogActivation Fresh() => JsonSerializer.Deserialize<RouteCatalogActivation>(input, json)!;
string Hash(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));
var actual = Fresh();
if (actual.AuxiliaryOperations?.Count != 85) throw new InvalidOperationException("The retained actual auxiliary inventory differs.");
string expected = RouteCatalogActivationBinding.ComputeDigest(actual);
if (BaselineBinding.ComputeDigest(Fresh()) != expected || CandidateBinding.ComputeDigest(Fresh()) != expected)
    throw new InvalidOperationException("Copied baseline/candidate digest differs from actual SDK binding.");
var scopeType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!;
var memoType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogValidatedBytesMemo")!;
var enterMemo = scopeType.GetMethod("EnterWithMemo", BindingFlags.Static | BindingFlags.NonPublic)!;
List<object> trials = [];
foreach (bool warm in new[] {false, true})
{
    using var memo = (IDisposable)Activator.CreateInstance(memoType, nonPublic: true)!;
    using (var seed = (IDisposable)enterMemo.Invoke(null, [memo])!) RouteCatalogCodec.Decode(actual.CanonicalCatalog, actual.RootDigest);
    Func<string>[] modes = [() => BaselineBinding.ComputeDigest(Fresh()), () => CandidateBinding.ComputeDigest(Fresh())];
    for (int iteration = 0; iteration < 10; iteration++)
    {
        foreach (int mode in iteration % 2 == 0 ? new[] {0,1} : new[] {1,0})
        {
            const int count = 20;
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            for (int i = 0; i < count; i++)
            {
                using var scope = warm ? (IDisposable)enterMemo.Invoke(null, [memo])! : null;
                if (modes[mode]() != expected) throw new InvalidOperationException("Digest changed during repeated fresh-input comparison.");
            }
            double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            trials.Add(new { warm, iteration, warmup = iteration < 2, mode = mode == 0 ? "preserved-baseline" : "per-call-lazy-concrete-inputs", operations = count,
                millisecondsPerOperation = elapsed / count, allocatedBytesPerOperation = (GC.GetAllocatedBytesForCurrentThread() - allocated) / count });
            Console.Error.WriteLine($"Completed mode={mode} warm={warm} trial={iteration}.");
        }
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { inputPath = args[0], inputSha256 = Hash(input), actualAuxiliaryCount = actual.AuxiliaryOperations.Count,
    expectedBindingDigest = expected, originalProductionAndCopiedFunctionsAgree = true, freshOwnedDeserializedInputPerOperation = true,
    authorityHostTrustStateLeasePermissionResultsRetained = false, runtimeNetworkCallsPerformed = false,
    profileBuild = new { configuration = "Debug", optimized = false, gcMode = System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation", note = "Standalone pure-computation model; not live host p99 or authority evidence." },
    actualSdkBinaries = new[] {typeof(RouteCatalogActivationBinding).Assembly, typeof(RouteCatalogCodec).Assembly}.Select(a => new {name=a.GetName().Name,path=a.Location,sha256=Hash(File.ReadAllBytes(a.Location))}), trials
}, new JsonSerializerOptions {WriteIndented = true}));
