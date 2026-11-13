using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
string gatewayPath = args[0];
var context = new OwnerContext(gatewayPath);
using var reflection = context.EnterContextualReflection();
Assembly gateway = context.LoadFromAssemblyPath(gatewayPath);
Assembly sdk = context.LoadFromAssemblyName(new("Hexalith.EventStore.ServiceDefaults"));
var assemblies = new HashSet<Assembly>();
var reasons = new Dictionary<string, List<string>>(StringComparer.Ordinal);
void Add(Assembly a, string reason)
{
    assemblies.Add(a);
    if (!reasons.TryGetValue(a.GetName().Name!, out var list)) reasons.Add(a.GetName().Name!, list = []);
    if (!list.Contains(reason)) list.Add(reason);
}
void TechnicalType(Type type)
{
    if (type.Assembly.GetName().Name!.StartsWith("Hexalith.EventStore", StringComparison.Ordinal)) Add(type.Assembly, "technical parameter/response type identity");
}
bool Marked(MemberInfo member) => member.IsDefined(context.LoadFromAssemblyName(new("Hexalith.EventStore.Contracts")).GetType("Hexalith.EventStore.Contracts.Routing.EventStoreTechnicalHttpControllerAttribute")!, false);
foreach (Type controller in gateway.GetTypes().Where(t => t.Namespace == "Hexalith.EventStore.Controllers" && typeof(ControllerBase).IsAssignableFrom(t)))
{
    foreach (MethodInfo method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => Marked(controller) || Marked(m)))
    {
        Add(controller.Assembly, "marked technical controller code");
        foreach (var parameter in method.GetParameters()) if (parameter.ParameterType != typeof(CancellationToken)) TechnicalType(parameter.ParameterType);
        foreach (var response in controller.GetCustomAttributes<ProducesResponseTypeAttribute>().Concat(method.GetCustomAttributes<ProducesResponseTypeAttribute>())) TechnicalType(response.Type);
        foreach (var filter in controller.GetCustomAttributes<ServiceFilterAttribute>().Concat(method.GetCustomAttributes<ServiceFilterAttribute>())) Add(filter.ServiceType.Assembly, "fresh service filter code");
    }
}
foreach (string name in new[] { "Hexalith.EventStore.Server", "Dapr.Actors.AspNetCore", "Dapr.Actors", "Microsoft.AspNetCore.Diagnostics.HealthChecks", "Dapr.AspNetCore", "Microsoft.AspNetCore.Mvc.Core", "Microsoft.AspNetCore.Routing", "Hexalith.EventStore.ServiceDefaults" }) Add(context.LoadFromAssemblyName(new(name)), "actual native/callback/constraint/global-filter code observer");
Assembly[] images = assemblies.OrderBy(a => a.FullName, StringComparer.Ordinal).ToArray();
var inventory = images.Select(a => new { assembly = a.GetName().Name, location = a.Location, bytes = new FileInfo(a.Location).Length, sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a.Location))), reasons = reasons[a.GetName().Name!] }).ToArray();
if (args[1] == "inventory") { File.WriteAllText(args[2], JsonSerializer.Serialize(new { imageCount = images.Length, imageBytes = inventory.Sum(x => x.bytes), chargedBytes = inventory.Sum(x => x.bytes + 512), inventory }, new JsonSerializerOptions { WriteIndented = true })); return; }
if (images.Length != 12) throw new InvalidOperationException($"Expected runtime-corroborated twelve-image inventory, found {images.Length}");
int slots = int.Parse(args[1]), workers = int.Parse(args[2]); string output = args[3];
var actionType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.TechnicalHttpActionObservation", true)!;
var identity = actionType.GetMethod("AssemblyIdentity", Flags, [typeof(Assembly), typeof(ArrayPool<byte>)])!.CreateDelegate<Identity>();
var memoType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogImageDigestMemo", true)!;
var scopeType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogImageDigestScope", true)!;
var enter = scopeType.GetMethod("Enter", Flags)!;
var hitsProperty = scopeType.GetProperty("CacheHits", Flags)!;
object memo = Activator.CreateInstance(memoType, true)!;
string[] expected = images.Select(identityAssembly => $"{identityAssembly.FullName}:{identityAssembly.ManifestModule.ModuleVersionId}:{inventory.Single(i => i.assembly == identityAssembly.GetName().Name).sha256}").ToArray();
var plain = ArrayPool<byte>.Create(16 * 1024 * 1024, slots);
object Run(ArrayPool<byte> pool, int iterations, bool alignEachInventory)
{
    using var start = new Barrier(workers + 1);
    using var each = new Barrier(workers);
    var outputRows = new WorkerResult[workers];
    Exception? failure = null;
    Thread[] threads = Enumerable.Range(0, workers).Select(worker => new Thread(() =>
    {
        try
        {
            using var contextual = context.EnterContextualReflection();
            start.SignalAndWait();
            long allocated = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
            double captureMs = 0; int allHits = 0;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                if (alignEachInventory) each.SignalAndWait();
                long captureStarted = Stopwatch.GetTimestamp();
                object scope = enter.Invoke(null, [memo])!;
                try
                {
                    for (int phase = 0; phase < 2; phase++)
                        for (int index = 0; index < images.Length; index++)
                            if (identity(images[index], pool) != expected[index]) throw new InvalidOperationException("Fresh image/MVID/SHA identity mismatch");
                    allHits += (int)hitsProperty.GetValue(scope)!;
                }
                finally { ((IDisposable)scope).Dispose(); }
                captureMs += Stopwatch.GetElapsedTime(captureStarted).TotalMilliseconds;
            }
            outputRows[worker] = new(worker, Stopwatch.GetElapsedTime(started).TotalMilliseconds, captureMs / iterations, GC.GetAllocatedBytesForCurrentThread() - allocated, allHits);
        }
        catch (Exception error) { Interlocked.CompareExchange(ref failure, error, null); }
    }) { IsBackground = true }).ToArray();
    foreach (var thread in threads) thread.Start();
    long beforeTotal = GC.GetTotalAllocatedBytes(true), beforeCpu = Process.GetCurrentProcess().TotalProcessorTime.Ticks;
    int[] beforeGc = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    long cohortStarted = Stopwatch.GetTimestamp();start.SignalAndWait();
    foreach (var thread in threads) thread.Join();
    double elapsed = Stopwatch.GetElapsedTime(cohortStarted).TotalMilliseconds;
    if (failure is not null) throw new InvalidOperationException("Worker failure", failure);
    return new { iterationsPerWorker = iterations, inventories = iterations * workers, observationsPerInventory = 2, imagesPerObservation = images.Length,
        elapsedMs = elapsed, throughputInventoriesPerSecond = iterations * workers * 1000 / elapsed,
        meanCaptureMs = outputRows.Average(x => x.MeanCaptureMs), allocatedBytesPerInventory = outputRows.Sum(x => x.AllocatedBytes) / (iterations * workers),
        processAllocatedBytes = GC.GetTotalAllocatedBytes(true) - beforeTotal, processCpuMs = (Process.GetCurrentProcess().TotalProcessorTime.Ticks - beforeCpu) / 10000d,
        gcDelta = Enumerable.Range(0, 3).Select(g => GC.CollectionCount(g) - beforeGc[g]).ToArray(), outputRows };
}
var cold = Run(plain, 1, false);
Run(plain, 8, false);
var samples = new List<object>();
for (int sample = 0; sample < 9; sample++) samples.Add(new { sample, values = Run(plain, 24, false) });
var aligned = Run(plain, 24, true);
var tracking = new TrackingPool(ArrayPool<byte>.Create(16 * 1024 * 1024, slots));
var trackingCold = Run(tracking, 1, false);var trackingColdCounts = tracking.Snapshot();
Run(tracking, 8, false);tracking.Reset();
var tracked = Run(tracking, 48, false);var trackedCounts = tracking.Snapshot();
var trackedAligned = Run(tracking, 24, true);var trackedAlignedCounts = tracking.Snapshot();
var receipt = new { diagnosticOnly = true, ownerSourcesEdited = false, hostsConstructed = false, credentialsRead = false,
    runtime = Environment.Version.ToString(), processorCount = Environment.ProcessorCount, processorOverride = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"), isServerGc = GCSettings.IsServerGC,
    slots, workers, inventory, imageCount = images.Length, fullFreshBytesPerInventory = inventory.Sum(x => x.bytes) * 2,
    SDK = new { path = sdk.Location, sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(sdk.Location))) },
    exactPath = "Actual SDK AssemblyIdentity(Assembly,ArrayPool<byte>) includes every original File.OpenRead/Length/ReadExactly, exact MemoryStream logical length, PE/MVID, actual SDK shared bounded memo and full-byte compare. One actual SDK private scope per complete initial+Complete pair, including disposal; shared memo survives loops. Reflection scope entry/hit access overhead is identical across capacities.",
    inventoryQualification = "Source-derived metadata census for marked Gateway action parameter/response/filter assemblies plus actual native/callback/constraint/global-filter code; no endpoint, DI or host construction. Expected 12 unique images corroborates runtime census; names/order were not retained in runtime timing. Sorted FullName order is used identically for both observations and all capacities.",
    trackingQualification = "Separate tracking samples wrap actual ArrayPool. Only per-rent thread allocation deltas and numeric counters; no allocated-array history or strong loan references retained. Primary timings use unwrapped actual ArrayPool. All worker threads are dedicated and start together; continuous loops have no per-image/per-inventory barriers. Separate aligned sample adds an inventory-start barrier.",
    cold, samples, aligned, trackingCold, trackingColdCounts, tracked, trackedCounts, trackedAligned, trackedAlignedCounts,
    memoCount = memoType.GetProperty("Count", Flags)!.GetValue(memo), memoRetainedChargedBytes = memoType.GetProperty("RetainedBytes", Flags)!.GetValue(memo) };
((IDisposable)memo).Dispose();
File.WriteAllText(output, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"slots={slots} workers={workers} complete {output}");

delegate string Identity(Assembly assembly, ArrayPool<byte> pool);
record WorkerResult(int Worker, double ElapsedMs, double MeanCaptureMs, long AllocatedBytes, int MemoHits);
sealed class TrackingPool(ArrayPool<byte> inner) : ArrayPool<byte>
{
    private long _rents, _rentBytes, _allocatingRents, _rentAllocated, _outstanding, _peak;
    public override byte[] Rent(int length)
    {
        long before = GC.GetAllocatedBytesForCurrentThread(); byte[] value = inner.Rent(length); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Interlocked.Increment(ref _rents);Interlocked.Add(ref _rentBytes, value.Length);Interlocked.Add(ref _rentAllocated, allocated);
        if (allocated >= value.Length) Interlocked.Increment(ref _allocatingRents);
        long current = Interlocked.Increment(ref _outstanding), previous;
        do { previous = Interlocked.Read(ref _peak); if (current <= previous) break; } while (Interlocked.CompareExchange(ref _peak, current, previous) != previous);
        return value;
    }
    public override void Return(byte[] array, bool clearArray = false) { inner.Return(array, clearArray); if (Interlocked.Decrement(ref _outstanding) < 0) throw new InvalidOperationException("Unbalanced pool return"); }
    public object Snapshot() => new { rents = _rents, rentedBytes = _rentBytes, allocatingRents = _allocatingRents, rentAllocatedBytes = _rentAllocated, outstandingLoans = _outstanding, peakLoans = _peak };
    public void Reset() { if (_outstanding != 0) throw new InvalidOperationException("Outstanding pool loan at reset");_rents = _rentBytes = _allocatingRents = _rentAllocated = _peak = 0; }
}
sealed class OwnerContext(string path) : AssemblyLoadContext("ordinary-gateway-pool-probe", false)
{
    private readonly AssemblyDependencyResolver _resolver = new(path);
    protected override Assembly? Load(AssemblyName name)
    {
        string? resolved = _resolver.ResolveAssemblyToPath(name);
        if (resolved is not null) return LoadFromAssemblyPath(resolved);
        string fallback = Path.Combine("/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/src/Hexalith.EventStore/bin/Debug/net10.0", name.Name + ".dll");
        return File.Exists(fallback) ? LoadFromAssemblyPath(fallback) : null;
    }
}
