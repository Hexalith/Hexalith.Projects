using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

string output = args[0];
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Development", ContentRootPath = AppContext.BaseDirectory });
builder.Logging.ClearProviders();
builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
builder.Services.AddEventStoreServer(builder.Configuration);
builder.Services.AddEventStoreHttpHostSecurity();
await using var app = builder.Build(); // Deliberately never Start/Run: metadata-only service and endpoint construction.
app.MapActorsHandlers().RequireEventStoreChannel();
var runtime = app.Services.GetRequiredService<ActorRuntime>();
var options = app.Services.GetRequiredService<IOptions<ActorRuntimeOptions>>();
var health = app.Services.GetRequiredService<HealthCheckService>();
var baseline = new DevelopmentNativeActorReceiver(runtime,options,health,builder.Configuration);
var candidate = new FormattingNativeActorReceiver(runtime,options,health,builder.Configuration);
var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s=>s.Endpoints).OfType<RouteEndpoint>().ToArray();
var actors = options.Value.Actors.ToArray();
var methods = actors.SelectMany(a=>a.Type.InterfaceTypes).SelectMany(t=>t.GetMethods()).ToArray();
var methodsAgain = actors.SelectMany(a=>a.Type.InterfaceTypes).SelectMany(t=>t.GetMethods()).ToArray();
string actorIdentity = string.Join('\n',actors.OrderBy(a=>a.Type.ActorTypeName,StringComparer.Ordinal).Select(a=>a.Type.ActorTypeName+"|"+a.Type.ImplementationType.FullName+"|"+a.Type.IsRemindable+"|"+string.Join(';',a.Type.InterfaceTypes.SelectMany(t=>t.GetMethods()).Select(m=>m.ToString()).Order(StringComparer.Ordinal))));
var memo = new RouteCatalogImageDigestMemo();
int parity=0;
RouteCatalogNativeActorTransport? Observe(string variant,RouteEndpoint endpoint,TechnicalHttpObservationPass pass,NativeFormattingMemo? formatting)
    => variant == "compiled-baseline" ? baseline.Observe(endpoint,pass) : candidate.Observe(endpoint,pass,formatting);
object OnePass(string variant)
{
    using var scope = RouteCatalogImageDigestScope.Enter(memo);
    var pass = new TechnicalHttpObservationPass(app.Services);
    NativeFormattingMemo? formatting = variant is "bounded-format" or "bounded-join" ? new() {JoinEnabled = variant == "bounded-join"} : null;
    foreach(var endpoint in endpoints)
    {
        var initial=Observe(variant,endpoint,pass,formatting) ?? throw new Exception("Actual endpoint rejected");
        string expected=TechnicalHttpActionObservation.Hash(initial);
        pass.RequireUnchanged(()=>TechnicalHttpActionObservation.Hash(Observe(variant,endpoint,pass,formatting)),expected);
        var second=Observe(variant,endpoint,pass,formatting);
        if(TechnicalHttpActionObservation.Hash(second)!=expected)throw new Exception("Observation differs");
    }
    pass.Complete();
    return new { imageHits = scope.CacheHits, methodHits = formatting?.Hits ?? 0, methodMisses = formatting?.Misses ?? 0, retainedFormattingBytes = formatting?.Bytes ?? 0 };
}
foreach(var endpoint in endpoints)
{
    var pass=new TechnicalHttpObservationPass(app.Services);
    var a=baseline.Observe(endpoint,pass);
    var b=candidate.Observe(endpoint,pass,new() {JoinEnabled=true});
    if(a!=b)throw new Exception("Complete actual Observe field parity failed"); parity++;
}
foreach(var endpoint in endpoints)
{
    var wrong=new RouteEndpoint(_=>Task.CompletedTask,endpoint.RoutePattern,endpoint.Order,endpoint.Metadata,endpoint.DisplayName);
    if(baseline.Observe(wrong)!=candidate.Observe(wrong))throw new Exception("Changed delegate parity failed");parity++;
}
// Fresh options and Complete callbacks must still observe a changed registered actor inventory.
using(var scope=RouteCatalogImageDigestScope.Enter(memo))
{
    var bp=new TechnicalHttpObservationPass(app.Services);var cp=new TechnicalHttpObservationPass(app.Services);var fm=new NativeFormattingMemo{JoinEnabled=true};
    var endpoint=endpoints[0];var b=baseline.Observe(endpoint,bp);var c=candidate.Observe(endpoint,cp,fm);
    string expected=TechnicalHttpActionObservation.Hash(b);
    if(b!=c)throw new Exception("Warm initial parity");parity++;
    bp.RequireUnchanged(()=>TechnicalHttpActionObservation.Hash(baseline.Observe(endpoint,bp)),expected);
    cp.RequireUnchanged(()=>TechnicalHttpActionObservation.Hash(candidate.Observe(endpoint,cp,fm)),expected);
    var removed=options.Value.Actors.Last();options.Value.Actors.Remove(removed.Type);
    try
    {
        b=baseline.Observe(endpoint,bp);c=candidate.Observe(endpoint,cp,fm);
        if(b!=c || TechnicalHttpActionObservation.Hash(b)==expected)throw new Exception("Fresh options mutation parity");parity++;
        string? Error(TechnicalHttpObservationPass p){try{p.Complete();return null;}catch(Exception e){return e.GetType().FullName+":"+e.Message;}}
        string? be=Error(bp),ce=Error(cp);if(be is null || be!=ce)throw new Exception("Fresh Complete mutation error parity");parity++;
    }
    finally{options.Value.Actors.Add(removed);}
}
// A different fresh method vector under the same RuntimeType key must never return old formatting.
var changeMemo=new NativeFormattingMemo{JoinEnabled=true};
foreach(Type contract in new[]{typeof(IDisposable),typeof(IAsyncDisposable),typeof(IDisposable)})
{
    string expected=string.Join(';',contract.GetMethods().Select(m=>m.ToString()).Order(StringComparer.Ordinal));
    if(changeMemo.Join(typeof(object),new[]{contract})!=expected)throw new Exception("Fresh method vector mismatch");parity++;
}
var variantNames=new[]{"compiled-baseline","copied-baseline","bounded-join"};
var firstRows=new List<object>();
foreach(string variant in variantNames)
{
    long a=GC.GetAllocatedBytesForCurrentThread(),t=Stopwatch.GetTimestamp();var details=OnePass(variant);
    firstRows.Add(new {variant,elapsedMs=Stopwatch.GetElapsedTime(t).TotalMilliseconds,allocatedBytes=GC.GetAllocatedBytesForCurrentThread()-a,details});
}
foreach(string variant in variantNames) for(int warmup=0;warmup<192;warmup++) OnePass(variant);
var rows=new List<object>();
const int iterations=96;
for(int sample=0;sample<12;sample++)
 foreach(string variant in variantNames.Skip(sample%3).Concat(variantNames.Take(sample%3)))
 {
    long a=GC.GetAllocatedBytesForCurrentThread(),total=GC.GetTotalAllocatedBytes(true),t=Stopwatch.GetTimestamp(),cpu=Process.GetCurrentProcess().TotalProcessorTime.Ticks;
    int[] g=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();object? details=null;
    for(int i=0;i<iterations;i++)details=OnePass(variant);
    rows.Add(new {sample,variant,iterations,elapsedMsPerPass=Stopwatch.GetElapsedTime(t).TotalMilliseconds/iterations,cpuMsPerPass=(Process.GetCurrentProcess().TotalProcessorTime.Ticks-cpu)/10000d/iterations,
      allocatedBytesPerPass=(GC.GetAllocatedBytesForCurrentThread()-a)/iterations,totalAllocatedBytes=GC.GetTotalAllocatedBytes(true)-total,gcDelta=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-g[i]).ToArray(),details});
 }
object Attr(Assembly a){var d=a.GetCustomAttribute<DebuggableAttribute>()!;return new {a.FullName,location=a.Location,sha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a.Location))),configuration=a.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,flags=d.DebuggingFlags.ToString(),d.IsJITOptimizerDisabled,d.IsJITTrackingEnabled};}
File.WriteAllText(output,JsonSerializer.Serialize(new {oracle=Attr(typeof(DevelopmentNativeActorReceiver).Assembly),candidate=Attr(Assembly.GetExecutingAssembly()),runtime=Environment.Version.ToString(),processorCount=Environment.ProcessorCount,serverGc=GCSettings.IsServerGC,
  noHostStarted=true,parityChecks=parity,endpointCount=endpoints.Length,actorCount=actors.Length,interfaceMethodCount=methods.Length,uniqueMethodRefs=methods.Distinct(ReferenceEqualityComparer.Instance).Count(),freshGetMethodsStableReferences=methods.Zip(methodsAgain).All(p=>ReferenceEquals(p.First,p.Second)),
  actorIdentityChars=actorIdentity.Length,actorIdentitySha256=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(actorIdentity))),actors=actors.Select(a=>new{a.Type.ActorTypeName,implementation=a.Type.ImplementationType.FullName,interfaces=a.Type.InterfaceTypes.Select(t=>t.FullName).ToArray()}),observesPerPass=18,completeFreshImages=4,firstTimedAfterParity=firstRows,rows},new JsonSerializerOptions{WriteIndented=true}));
memo.Dispose();
