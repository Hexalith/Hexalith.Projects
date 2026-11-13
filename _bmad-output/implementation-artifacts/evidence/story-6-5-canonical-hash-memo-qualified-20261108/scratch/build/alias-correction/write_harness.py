from pathlib import Path
P=Path('/tmp/story65-canonical-hash-memo-prep-20261108/harness')
def put(name,text): (P/name).write_text(text.strip()+'\n')
put('IVariant.cs',r'''
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>Matches one per-authority owner and one fresh closing scope per observation.</summary>
internal interface IVariant : IDisposable
{
    string Name { get; }
    IDisposable Enter();
    object Capture(RouteCatalogHostObservation observation, string workload);
    string[] Digests(RouteCatalogHostObservation observation);
    object Counters();
}
''')
for variant,typ,ns in [('Production','ProductionVariant','Hexalith.EventStore.ServiceDefaults.Routing'),('Control','ControlVariant','Story65.Canonical.Control'),('Candidate','CandidateVariant','Story65.Canonical.Candidate')]:
 scope='Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope' if variant=='Production' else 'Story65.Canonical.'+variant+'.RouteCatalogDecodeScope'
 field='\n    private readonly Story65.Canonical.Candidate.RouteCatalogCanonicalHashMemo _hashes = new();' if variant=='Candidate' else ''
 enter=scope+'.EnterWithCanonicalHashMemo(_bytes, _hashes)' if variant=='Candidate' else scope+'.EnterWithMemo(_bytes)'
 dispose='_hashes.Dispose(); ' if variant=='Candidate' else ''
 counters='new { _hashes.Count, _hashes.RetainedCharge, _hashes.Hits, _hashes.Misses, _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes }' if False else ('new { HashEntries = _hashes.Count, _hashes.RetainedCharge, _hashes.Hits, _hashes.Misses, ByteMemoEntries = _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes }' if variant=='Candidate' else 'new { ByteMemoEntries = _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes }')
 put(typ+'.cs',f'''
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Inventory = {ns}.ObservedRouteCatalogInventory;
using Host = {ns}.ObservedRouteCatalogHost;
namespace Story65.Canonical.Harness;
/// <summary>{variant} complete inventory and identity path, with the same original byte-memo scope ownership.</summary>
internal sealed class {typ} : IVariant
{{
    private readonly RouteCatalogValidatedBytesMemo _bytes = new();{field}
    public string Name => "{variant.lower()}";
    public IDisposable Enter() => {enter};
    public object Capture(RouteCatalogHostObservation observation, string workload)
        => workload == "identity" ? Host.Identity(observation) : new Inventory(observation).Observation;
    public string[] Digests(RouteCatalogHostObservation observation)
    {{
        var inventory = new Inventory(observation);
        var owned = inventory.Observation;
        return [.. owned.AdmissionPolicies!.Select(inventory.Admission), .. owned.Declarations.Select(inventory.Declaration),
            .. (owned.Operations ?? []).Select(inventory.Operation)];
    }}
    public object Counters() => {counters};
    public void Dispose() {{ {dispose}_bytes.Dispose(); }}
}}
''')
put('Variants.cs',r'''
namespace Story65.Canonical.Harness;
/// <summary>Constructs independent owners without cross-variant memo sharing.</summary>
internal static class Variants
{
    internal static IVariant Create(string name) => name switch
    {
        "production" => new ProductionVariant(), "control" => new ControlVariant(), "candidate" => new CandidateVariant(),
        _ => throw new ClosedFailure("unknown-variant"),
    };
}
''')
put('ClosedFailure.cs',r'''
using System.Runtime.CompilerServices;
namespace Story65.Canonical.Harness;
/// <summary>Persists only closed invariant labels and source coordinates.</summary>
internal sealed class ClosedFailure(string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : Exception
{
    internal string Check { get; } = check;
    internal string AssertionFile { get; } = Path.GetFileName(file);
    internal int AssertionLine { get; } = line;
}
''')
put('Check.cs',r'''
using System.Runtime.CompilerServices;
namespace Story65.Canonical.Harness;
/// <summary>Stops a scratch experiment on a bounded invariant.</summary>
internal static class Check
{
    internal static void That(bool value, string check, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    { if (!value) throw new ClosedFailure(check, file, line); }
}
''')
put('Dataset.cs',r'''
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>A public compiled-owner or checked-in fixture inventory; not a live complete host inventory.</summary>
internal sealed record Dataset(string Name, RouteCatalogHostObservation Observation)
{
    internal RouteCatalogHostObservation Fresh() => Observation with
    {
        Declarations = Observation.Declarations.ToArray(), Targets = Observation.Targets.ToArray(),
        AdmissionPolicies = Observation.AdmissionPolicies!.ToArray(), Operations = Observation.Operations?.ToArray(),
        RequiredHostIds = Observation.RequiredHostIds.ToArray(), TopologyDocuments = Observation.TopologyDocuments.Select(bytes => bytes.ToArray()).ToArray(),
        AuthenticationProfile = Observation.AuthenticationProfile.ToArray(),
    };
}
''')
put('Datasets.cs',r'''
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Sample.Counter;
namespace Story65.Canonical.Harness;
/// <summary>Reads actual compiled Sample contracts and the unchanged public routing fixture, without hosts or service providers.</summary>
internal static class Datasets
{
    internal const string Root = "/home/administrator/projects/hexalith/projects";
    internal const string Fixture = Root + "/references/Hexalith.EventStore/deploy/dapr/eventstore-routing-catalog.json";
    internal static Dataset[] Load()
    {
        RouteCatalogEnvelope fixture = RouteCatalogCodec.Decode(File.ReadAllBytes(Fixture));
        var sampleDeclarations = CompiledRouteCatalogDeclarations.Read([typeof(SampleCatalog).Assembly]);
        var sampleOperations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(SampleCatalog).Assembly]);
        Check.That(sampleDeclarations.Count > 0 && sampleOperations.Count == sampleDeclarations.Count, "compiled-operation-completeness");
        return [Build("public-catalog-fixture", fixture.Declarations, fixture.Idempotency, null, fixture.Routes),
            Build("compiled-sample-owner", sampleDeclarations, [], sampleOperations, [])];
    }
    private static Dataset Build(string name, IReadOnlyList<RouteContractDeclaration> declarations,
        IReadOnlyList<IdempotencyCatalogEntry> admission, IReadOnlyList<RouteOperationContract>? operations, IReadOnlyList<RouteCatalogEntry> targets)
    {
        var observation = new RouteCatalogHostObservation("scratch-host", "scratch-app", declarations, targets, ["scratch-host"],
            [JsonSerializer.SerializeToUtf8Bytes(new { kind = "Component", metadata = new { name = "scratch-state" } }),
             JsonSerializer.SerializeToUtf8Bytes(new { spec = new { accessControl = new { defaultAction = "deny" } } })],
            "synthetic-auth-profile"u8.ToArray(), admission, [], [], new("scratch-state", "scratch-key", "http://127.0.0.1"), Operations: operations);
        return new(name, observation);
    }
    internal static object Facts(Dataset dataset) => new
    {
        dataset.Name, Declarations = dataset.Observation.Declarations.Count, AdmissionPolicies = dataset.Observation.AdmissionPolicies!.Count,
        Operations = dataset.Observation.Operations?.Count ?? 0,
        Source = dataset.Name == "compiled-sample-owner" ? "actual-compiled-SampleCatalog" : "unchanged-public-catalog-fixture",
        NoCompleteLiveHostInventoryClaim = true,
    };
    internal static string Sha(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
''')
put('CaptureSample.cs',r'''
namespace Story65.Canonical.Harness;
/// <summary>One whole scoped observation elapsed wall time, without request or payload identity.</summary>
internal sealed record CaptureSample(int Worker, int Ordinal, long ElapsedTicks);
''')
put('BatchSample.cs',r'''
namespace Story65.Canonical.Harness;
/// <summary>One shared-owner batch; batch wall divided by observations is amortized throughput cost.</summary>
internal sealed record BatchSample(string Dataset, string Workload, string State, string Variant, int Readers, int Round,
    int Observations, long WallTicks, double CpuMilliseconds, long AllocatedBytes, int Gen0, int Gen1, int Gen2,
    CaptureSample[] Captures, object Counters);
''')
put('BatchRunner.cs',r'''
using System.Diagnostics;
namespace Story65.Canonical.Harness;
/// <summary>Times whole fresh input capture, original clone/validation/serialization and fresh scope entry/disposal.</summary>
internal static class BatchRunner
{
    internal static async Task<BatchSample> Run(Dataset dataset, string workload, string state, string variant, int readers, int round)
    {
        using IVariant owner = Variants.Create(variant);
        if (state == "warm")
        {
            using var scope = owner.Enter();
            GC.KeepAlive(owner.Capture(dataset.Fresh(), workload));
        }
        int iterations = state == "warm" ? 4 : 1;
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
        return new(dataset.Name, workload, state, variant, readers, round, readers * iterations, wall, cpu,
            GC.GetTotalAllocatedBytes(precise: true) - allocated, GC.CollectionCount(0)-g0, GC.CollectionCount(1)-g1, GC.CollectionCount(2)-g2,
            workers.SelectMany(samples => samples).ToArray(), owner.Counters());
    }
}
''')
put('BenchmarkDriver.cs',r'''
using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Balances all six variant permutations; compares entire operations, not canonical-writer-only loops.</summary>
internal static class BenchmarkDriver
{
    internal static async Task<BatchSample[]> Run(Dataset[] datasets, string output)
    {
        string[][] orders = [["production","control","candidate"], ["production","candidate","control"],
            ["control","production","candidate"], ["control","candidate","production"],
            ["candidate","production","control"], ["candidate","control","production"]];
        var all = new List<BatchSample>();
        using var writer = new StreamWriter(Path.Combine(output, "raw-batches.jsonl"), append: false);
        foreach (var dataset in datasets)
        foreach (string workload in new[] { "inventory", "identity" })
        foreach (string state in new[] { "memo-cold", "warm" })
        foreach (int readers in new[] { 1, 2, 4 })
        for (int round = 0; round < orders.Length; round++)
        foreach (string variant in orders[round])
        {
            BatchSample sample = await BatchRunner.Run(dataset, workload, state, variant, readers, round+1);
            all.Add(sample);
            await writer.WriteLineAsync(JsonSerializer.Serialize(sample));
            await writer.FlushAsync();
        }
        Check.That(all.Count == 432 && all.Sum(sample => sample.Observations) == 2520, "complete-balanced-benchmark-matrix");
        return all.ToArray();
    }
}
''')
put('Summary.cs',r'''
using System.Diagnostics;
namespace Story65.Canonical.Harness;
/// <summary>Reports independent observation latency separately from amortized batch throughput costs.</summary>
internal static class Summary
{
    internal static object[] Build(BatchSample[] all) => all.GroupBy(sample => new {sample.Dataset,sample.Workload,sample.State,sample.Variant,sample.Readers})
        .Select(group => (object)new
        {
            group.Key, Batches=group.Count(), Observations=group.Sum(sample=>sample.Observations),
            MedianAmortizedWallMilliseconds = Median(group.Select(sample=>Ms(sample.WallTicks)/sample.Observations)),
            MedianCpuMillisecondsPerObservation=Median(group.Select(sample=>sample.CpuMilliseconds/sample.Observations)),
            MedianAllocatedBytesPerObservation=Median(group.Select(sample=>(double)sample.AllocatedBytes/sample.Observations)),
            ObservationMedianMilliseconds=Median(group.SelectMany(sample=>sample.Captures).Select(sample=>Ms(sample.ElapsedTicks))),
            ObservationP95Milliseconds=Percentile(group.SelectMany(sample=>sample.Captures).Select(sample=>Ms(sample.ElapsedTicks)), .95),
        }).ToArray();
    private static double Ms(long ticks)=>1000.0*ticks/Stopwatch.Frequency;
    private static double Median(IEnumerable<double> values) { double[] x=values.Order().ToArray(); return (x[(x.Length-1)/2]+x[x.Length/2])/2; }
    private static double Percentile(IEnumerable<double> values,double p) { double[] x=values.Order().ToArray(); return x[(int)Math.Ceiling(x.Length*p)-1]; }
}
''')
put('ChangingPreimage.cs',r'''
namespace Story65.Canonical.Harness;
/// <summary>A test-only serialization observation proves misses never serialize mutable original input twice.</summary>
internal sealed class ChangingPreimage
{
    private int _reads;
    public int Value => ++_reads;
    internal int Reads => _reads;
}
''')
put('RuntimeFacts.cs',r'''
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>Requires unoptimized Debug, natural14 .NET10.0.14 and actual producer identities.</summary>
internal static class RuntimeFacts
{
    internal static object Validate()
    {
        Check.That(Environment.Version == new Version(10,0,14) && Environment.ProcessorCount == 14, "runtime-and-natural-fourteen");
        Check.That(Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") is null && GCSettings.IsServerGC, "natural-cpu-server-gc");
        return new { Runtime=Environment.Version.ToString(),Environment.ProcessorCount,GCSettings.IsServerGC,
            Stopwatch.Frequency,Harness=AssemblyFacts(Assembly.GetExecutingAssembly()),
            ActualContracts=AssemblyFacts(typeof(RouteCatalogCodec).Assembly),ActualSdk=AssemblyFacts(typeof(ObservedRouteCatalogHost).Assembly),
            ForcedGcCalls=0,NewRuntimeEnvironmentValues=0 };
    }
    private static object AssemblyFacts(Assembly assembly)
    {
        var flags=assembly.GetCustomAttribute<DebuggableAttribute>();
        Check.That(flags?.IsJITOptimizerDisabled == true, "debug-disable-optimizations");
        return new {assembly.FullName,Sha256=Datasets.Sha(assembly.Location),Debuggable=flags.DebuggingFlags.ToString()};
    }
}
''')
put('Program.cs',r'''
using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Runs only after an exact source/compiled-input manifest and separate Root release.</summary>
internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        Check.That(args.Length == 3, "manifest-sha-output-arguments");
        string output=Path.GetFullPath(args[2]);
        Check.That(!Directory.Exists(output), "fresh-output-directory"); Directory.CreateDirectory(output);
        try
        {
            InputPins.Verify(args[0],args[1]);
            object facts=RuntimeFacts.Validate();
            File.WriteAllText(Path.Combine(output,"runtime-facts.json"),JsonSerializer.Serialize(facts,new JsonSerializerOptions {WriteIndented=true}));
            Dataset[] datasets=Datasets.Load();
            File.WriteAllText(Path.Combine(output,"dataset-facts.json"),JsonSerializer.Serialize(datasets.Select(Datasets.Facts),new JsonSerializerOptions {WriteIndented=true}));
            object parity=await Parity.Run(datasets);
            File.WriteAllText(Path.Combine(output,"parity.json"),JsonSerializer.Serialize(parity,new JsonSerializerOptions {WriteIndented=true}));
            BatchSample[] raw=await BenchmarkDriver.Run(datasets,output);
            File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(Summary.Build(raw),new JsonSerializerOptions {WriteIndented=true}));
            InputPins.Verify(args[0],args[1]);
            File.WriteAllText(Path.Combine(output,"closed.json"),JsonSerializer.Serialize(new {Passed=true,Batches=raw.Length,Observations=raw.Sum(x=>x.Observations),ProductionApproval=false,HostDeadlineClaim=false}));
            return 0;
        }
        catch(Exception error)
        {
            var closed=error as ClosedFailure;
            File.WriteAllText(Path.Combine(output,"failure.json"),JsonSerializer.Serialize(new {ErrorType=error.GetType().FullName,closed?.Check,closed?.AssertionFile,closed?.AssertionLine}));
            return 1;
        }
    }
}
''')
put('InputPins.cs',r'''
using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Rehashes exact Root-reviewed current inputs, with no historical SDK fallback.</summary>
internal static class InputPins
{
    internal static void Verify(string path,string expected)
    {
        Check.That(expected.Length==64 && Datasets.Sha(path)==expected,"runtime-manifest-hash");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));
        var root=doc.RootElement;
        Check.That(root.GetProperty("executionReleased").GetBoolean(),"separate-runtime-release");
        foreach(var item in root.GetProperty("inputsSha256").EnumerateObject())
        {
            Check.That(Path.IsPathFullyQualified(item.Name) && !new FileInfo(item.Name).LinkTarget?.Any()==true,"absolute-regular-input-coordinate");
            Check.That(File.Exists(item.Name) && Datasets.Sha(item.Name)==item.Value.GetString(),"frozen-current-input");
        }
        foreach(var item in root.GetProperty("compiledInputsSha256").EnumerateObject())
            Check.That(File.Exists(item.Name) && Datasets.Sha(item.Name)==item.Value.GetString(),"frozen-compiled-input");
    }
}
''')
