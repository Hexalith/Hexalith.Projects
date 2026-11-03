using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var scopeType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!;
var enterMemo = scopeType.GetMethod("EnterWithMemo", BindingFlags.Static | BindingFlags.NonPublic)!;
var memoType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogValidatedBytesMemo")!;
var results = new List<object>();
var proof = new Dictionary<string, object>();
string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
async Task Measure(string name, int count, Func<Task> operation)
{
    for (int i=0; i<12; i++) await operation();
    List<double> times=[]; long bytes=GC.GetTotalAllocatedBytes(true);
    for (int i=0; i<count; i++) {long started=Stopwatch.GetTimestamp();await operation();times.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);}
    bytes=GC.GetTotalAllocatedBytes(true)-bytes;times.Sort();
    results.Add(new {name,operations=count,medianMilliseconds=times[count/2],p95Milliseconds=times[(int)(count*.95)],averageAllocatedBytes=bytes/count});
}
byte[] catalog=File.ReadAllBytes(args[0]);
string root=Hash(catalog);
byte[] activationBytes=File.ReadAllBytes(args[1]);
var activation=JsonSerializer.Deserialize<RouteCatalogActivation>(activationBytes,options)!;
if (activation.RootDigest!=root || !catalog.AsSpan().SequenceEqual(activation.CanonicalCatalog)) throw new Exception("Input binding differs");
var expectedCatalog=RouteCatalogCodec.Decode(catalog,root);
using (var memo=(IDisposable)Activator.CreateInstance(memoType,nonPublic:true)!)
{
    using (var seed=(IDisposable)enterMemo.Invoke(null,[memo])!) RouteCatalogCodec.Decode(catalog,root);
    await Measure("seven-exact-byte-decodes-owned-memo",80,()=> {
        using var scope=(IDisposable)enterMemo.Invoke(null,[memo])!;
        for (int i=0;i<7;i++) {var actual=RouteCatalogCodec.Decode(catalog,root);if(actual.RouteFacetDigest!=expectedCatalog.RouteFacetDigest || actual.IdempotencyFacetDigest!=expectedCatalog.IdempotencyFacetDigest)throw new Exception("Facet mismatch");}
        return Task.CompletedTask;
    });
    await Measure("fresh-release-binding-owned-memo",80,()=> {using var scope=(IDisposable)enterMemo.Invoke(null,[memo])!;RouteCatalogActivationBinding.ComputeDigest(activation);return Task.CompletedTask;});
}
if (!RouteCatalogCodec.Encode(RouteCatalogCodec.Decode(catalog,root)).AsSpan().SequenceEqual(catalog)) throw new Exception("Canonical mismatch");
proof["catalog"]=new {bytes=catalog.Length,root,canonicalBytesAndFacetsUnchanged=true,activationInputSha256=Hash(activationBytes)};
foreach (string input in args.Skip(2))
{
    byte[] inputBytes=File.ReadAllBytes(input);var observation=JsonSerializer.Deserialize<RouteCatalogHostObservation>(inputBytes,options)!;
    var expected=ObservedRouteCatalogHost.Identity(observation);
    var observer=new ObservedRouteCatalogHost(_=>Task.FromResult(JsonSerializer.Deserialize<RouteCatalogHostObservation>(inputBytes,options)!));
    using var memo=(IDisposable)Activator.CreateInstance(memoType,nonPublic:true)!;
    using(var seed=(IDisposable)enterMemo.Invoke(null,[memo])!) RouteCatalogCodec.Decode(catalog,root);
    await Measure("fresh-capture-and-observe-"+observation.AppId,60,async()=> {
        using var scope=(IDisposable)enterMemo.Invoke(null,[memo])!;RouteCatalogCodec.Decode(catalog,root);
        var actual=await observer.ObserveAsync(activation,observation.AuthorityCoordinates!,default);
        if(actual!=expected)throw new Exception("Observation identity differs");
    });
    proof[observation.AppId]=new{inputSha256=Hash(inputBytes),identitySha256=Hash(JsonSerializer.SerializeToUtf8Bytes(expected)),freshCapturePerOperation=true};
}
Assembly tests=Assembly.Load("Hexalith.Projects.Server.Tests");
Type fixtureType=tests.GetType("Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadFixture")!;
Task create=(Task)fixtureType.GetMethod("CreateAsync")!.Invoke(null,[true,false,false])!;await create;
object fixture=create.GetType().GetProperty("Result")!.GetValue(create)!;
foreach((string id,long position) in new[]{("project-a",100L),("project-b",200L)})await (Task)fixtureType.GetMethod("SeedAsync")!.Invoke(fixture,[id,position,true])!;
var executor=(ProjectReadQueryExecutor)fixtureType.GetProperty("Executor")!.GetValue(fixture)!;
QueryEnvelope Query(object payload,string id="projects") => (QueryEnvelope)fixtureType.GetMethod("Query")!.Invoke(fixture,[payload,id,"profile-read"])!;
string? truth=null;
await Measure("real-query-executor-list-continuation-open-in-memory-fixture",120,async()=> {
    QueryResult a=await executor.ListAsync(Query(new ListProjectsQuery(PageSize:1)),default);
    if(!a.Success)throw new Exception("List failed");
    var first=JsonSerializer.Deserialize<ProjectListPage>(a.PayloadBytes!,options)!;
    QueryResult b=await executor.ListAsync(Query(new ListProjectsQuery(Cursor:first.Cursor,PageSize:1)),default);
    QueryResult c=await executor.GetAsync(Query(new GetProjectQuery(),"project-a"),default);
    if(!b.Success||!c.Success)throw new Exception("Continuation/open failed");
    var second=JsonSerializer.Deserialize<ProjectListPage>(b.PayloadBytes!,options)!;
    var opened=JsonSerializer.Deserialize<GetProjectResponse>(c.PayloadBytes!,options)!;
    if(first.Items.Single().ProjectId!="project-a" || !first.HasMore || first.Cursor is null || second.Items.Single().ProjectId!="project-b" || second.HasMore || opened.ProjectId!="project-a")throw new Exception("Query result differs");
    string current=Hash(JsonSerializer.SerializeToUtf8Bytes(new{first.Items,first.HasMore,SecondItems=second.Items,SecondHasMore=second.HasMore,opened},options));
    truth ??=current;if(truth!=current)throw new Exception("Query truth changed");
});
proof["queryFixture"]=new{normalizedTruthSha256=truth,authorityAndFolders="existing in-memory test fixture only; no live authority evidence",clock="existing fixed fixture clock; not a live source or authority refresh"};
var binaries=Directory.GetFiles(AppContext.BaseDirectory,"Hexalith.*.dll").Order(StringComparer.Ordinal).Select(path=> {
    var assembly=Assembly.LoadFrom(path);var debug=assembly.GetCustomAttribute<DebuggableAttribute>();string pdb=Path.ChangeExtension(path,".pdb");
    return new{name=assembly.GetName().Name,sha256=Hash(File.ReadAllBytes(path)),debuggingFlags=debug?.DebuggingFlags.ToString(),jitOptimizerDisabled=debug?.IsJITOptimizerDisabled,portablePdbPresent=File.Exists(pdb)&&File.ReadAllBytes(pdb).AsSpan(0,4).SequenceEqual("BSJB"u8)};
}).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new{proof,results,binaries,liveAuthorityStateHostTrustLeaseOrRuntimeCallsPerformed=false},new JsonSerializerOptions{WriteIndented=true}));
