using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],EnvironmentName="Development",ContentRootPath=AppContext.BaseDirectory});
builder.Logging.ClearProviders();builder.Host.UseDefaultServiceProvider(o=>o.ValidateOnBuild=false);
builder.Configuration["EventStore:DomainService:AppId"]="metadata-probe";
builder.Services.AddEventStoreServer(builder.Configuration);builder.Services.AddEventStoreHttpHostSecurity();
await using var app=builder.Build(); // Never Start/Run; metadata-only fixture with no Dapr transport use.
app.MapActorsHandlers().RequireEventStoreChannel();
var actorOptions=app.Services.GetRequiredService<IOptions<ActorRuntimeOptions>>();
var recordingOptions=new ProbeActorOptions(actorOptions);
var native=new DevelopmentNativeActorReceiver(app.Services.GetRequiredService<ActorRuntime>(),recordingOptions,app.Services.GetRequiredService<HealthCheckService>(),builder.Configuration);
var receiver=new TechnicalHttpRouteCatalogReceiver();
typeof(TechnicalHttpRouteCatalogReceiver).GetField("_services",Flags)!.SetValue(receiver,app.Services);
typeof(TechnicalHttpRouteCatalogReceiver).GetField("_native",Flags)!.SetValue(receiver,native);
var fingerprint=typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("Fingerprint",Flags)!.CreateDelegate<Fingerprint>();
var requestFingerprint=typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("RequestFingerprint",Flags)!.CreateDelegate<RequestFingerprint>();
var options=new RouteCatalogHostOptions{Profile="Development",DevelopmentFixture=true,ScopedDecodeReuse=true,RequireTechnicalHttpBinding=true,RetainValidatedCatalogBytes=true};
var store=Substitute.For<IRouteCatalogStateStore>();var trust=Substitute.For<IRouteCatalogTrustValidator>();
using var owner=new RouteCatalogAuthority(store,trust,Options.Create(options),null,null,builder.Environment);
var ownerMemo=(RouteCatalogImageDigestMemo)typeof(RouteCatalogAuthority).GetField("_imageDigests",Flags)!.GetValue(owner)!;
var ownedReceiver=new TechnicalHttpRouteCatalogReceiver();
typeof(TechnicalHttpRouteCatalogReceiver).GetField("_services",Flags)!.SetValue(ownedReceiver,app.Services);
typeof(TechnicalHttpRouteCatalogReceiver).GetField("_native",Flags)!.SetValue(ownedReceiver,native);
ownedReceiver.AttachPrivateImageOwner(owner);
var ordinaryBegin=typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("BeginPrivateImagePrecheck",Flags)!.CreateDelegate<Func<RouteCatalogImageDigestScope?>>(receiver);
var ownedBegin=typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("BeginPrivateImagePrecheck",Flags)!.CreateDelegate<Func<RouteCatalogImageDigestScope?>>(ownedReceiver);
var endpoints=((IEndpointRouteBuilder)app).DataSources.SelectMany(s=>s.Endpoints).OfType<RouteEndpoint>().ToArray();
var methodEndpoint=endpoints.Single(e=>e.RoutePattern.RawText=="actors/{actorTypeName}/{actorId}/method/{methodName}");
var actor=actorOptions.Value.Actors.First(a=>a.Type.InterfaceTypes.SelectMany(t=>t.GetMethods()).Any());
string methodName=actor.Type.InterfaceTypes.SelectMany(t=>t.GetMethods()).First().Name;
DefaultHttpContext Context(RouteEndpoint endpoint)
{
    var context=new DefaultHttpContext();context.SetEndpoint(endpoint);context.RequestServices=app.Services;
    context.Request.Method=endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault()??"GET";
    var a=endpoint.RoutePattern.RawText!.Contains("remind/",StringComparison.Ordinal)?actorOptions.Value.Actors.First(a=>a.Type.IsRemindable):actor;
    context.Request.RouteValues["actorTypeName"]=a.Type.ActorTypeName;context.Request.RouteValues["actorId"]="probe-owned";
    context.Request.RouteValues["methodName"]=methodName;context.Request.RouteValues["reminderName"]="probe";context.Request.RouteValues["timerName"]="probe";
    return context;
}
(string Fingerprint,bool Allows,int ImageHits) Prechecks(RouteEndpoint endpoint,DefaultHttpContext context,bool enabled)
{
    recordingOptions.Reset();
    var currentReceiver=enabled?ownedReceiver:receiver;
    RouteCatalogNativeActorTransport? observed;string? initial;bool allows;
    using(var boundary=RouteCatalogImageDigestScope.Suppress())
    using(var images=enabled?ownedBegin():ordinaryBegin())
    {
        var pass=new TechnicalHttpObservationPass(app.Services);
        observed=native.Observe(endpoint,pass);
        initial=observed is not null?fingerprint(currentReceiver,endpoint,pass,out _):null;
        pass.Complete();
        allows=observed is not null&&native.Allows(context);
    }
    if(observed is null||initial is null)throw new Exception("Actual native precheck refused");
    if(observed.Kind is "config" or "health")return(initial,allows,recordingOptions.Hits());
    // The FINAL compiled RequestFingerprint owns its actual second suppression/borrow/Complete region.
    string? requested=requestFingerprint(currentReceiver,endpoint,out _);
    if(requested!=initial)throw new Exception("Full actual RequestFingerprint differs");
    return(requested!,allows,recordingOptions.Hits());
}
int parity=0;var parityRows=new List<object>();
// The first complete owned request is the cold host-memo measurement, including four private image captures.
var coldContext=Context(methodEndpoint);
long coldAllocated=GC.GetAllocatedBytesForCurrentThread(),coldStarted=Stopwatch.GetTimestamp();
var cold=Prechecks(methodEndpoint,coldContext,true);
var coldResult=new{elapsedMs=Stopwatch.GetElapsedTime(coldStarted).TotalMilliseconds,allocatedBytes=GC.GetAllocatedBytesForCurrentThread()-coldAllocated,cold.ImageHits,ownerMemo.Count,ownerMemo.RetainedBytes};
foreach(var endpoint in endpoints)
{
    var context=Context(endpoint);var a=Prechecks(endpoint,context,false);var b=Prechecks(endpoint,context,true);
    if(a.Fingerprint!=b.Fingerprint||a.Allows!=b.Allows||!a.Allows)throw new Exception("Native endpoint/fingerprint/Allows parity");parity++;
    parityRows.Add(new{path=endpoint.RoutePattern.RawText,a.Allows,ownedHits=b.ImageHits});
}
using(var foreignMemo=new RouteCatalogImageDigestMemo())
using(var foreign=RouteCatalogImageDigestScope.Enter(foreignMemo))
{
    var a=Prechecks(methodEndpoint,coldContext,false);if(foreignMemo.Count!=0||RouteCatalogImageDigestScope.Current!=foreign)throw new Exception("Fallback borrowed foreign scope");parity++;
    var b=Prechecks(methodEndpoint,coldContext,true);if(foreignMemo.Count!=0||RouteCatalogImageDigestScope.Current!=foreign||a.Fingerprint!=b.Fingerprint)throw new Exception("Owner borrowed foreign scope");parity++;
}
options.RetainValidatedCatalogBytes=false;
var disabled=Prechecks(methodEndpoint,coldContext,true);if(disabled.ImageHits!=0||disabled.Fingerprint!=cold.Fingerprint)throw new Exception("Disabled fallback");parity++;
options.RetainValidatedCatalogBytes=true;
for(int warmup=0;warmup<48;warmup++){Prechecks(methodEndpoint,coldContext,false);Prechecks(methodEndpoint,coldContext,true);}
object Cohort(int workers,bool enabled,int iterations)
{
    using var start=new Barrier(workers+1);var times=new double[workers];var allocated=new long[workers];var hits=new int[workers];Exception? failure=null;
    var threads=Enumerable.Range(0,workers).Select(worker=>new Thread(()=>
    {
        try
        {
            var context=Context(methodEndpoint);start.SignalAndWait();long bytes=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();
            for(int iteration=0;iteration<iterations;iteration++)
            {
                var observed=Prechecks(methodEndpoint,context,enabled);
                if(observed.Fingerprint!=cold.Fingerprint||!observed.Allows)throw new Exception("Concurrent full-precheck parity");hits[worker]+=observed.ImageHits;
            }
            times[worker]=Stopwatch.GetElapsedTime(began).TotalMilliseconds;allocated[worker]=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
        catch(Exception e){Interlocked.CompareExchange(ref failure,e,null);}
    }){IsBackground=true}).ToArray();
    foreach(var thread in threads)thread.Start();
    long allAllocated=GC.GetTotalAllocatedBytes(true),cpu=Process.GetCurrentProcess().TotalProcessorTime.Ticks,began=Stopwatch.GetTimestamp();
    int[] gc=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();start.SignalAndWait();foreach(var thread in threads)thread.Join();
    if(failure is not null)throw new Exception("Concurrent full-precheck failure",failure);
    return new{workers,enabled,iterationsPerWorker=iterations,requests=iterations*workers,elapsedMs=Stopwatch.GetElapsedTime(began).TotalMilliseconds,meanRequestMs=times.Average()/iterations,
       allocatedBytesPerRequest=allocated.Sum()/(workers*iterations),processAllocatedBytes=GC.GetTotalAllocatedBytes(true)-allAllocated,processCpuMs=(Process.GetCurrentProcess().TotalProcessorTime.Ticks-cpu)/10000d,
       gcDelta=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-gc[i]).ToArray(),imageHits=hits.Sum(),workerElapsedMs=times};
}
var rows=new List<object>();
foreach(int workers in new[]{2,4})
{
    Cohort(workers,false,12);Cohort(workers,true,12);
    for(int sample=0;sample<9;sample++)foreach(bool enabled in sample%2==0?new[]{false,true}:new[]{true,false})rows.Add(new{sample,result=Cohort(workers,enabled,24)});
}
int warmCount=ownerMemo.Count,warmBytes=ownerMemo.RetainedBytes;
owner.Dispose();var closed=Prechecks(methodEndpoint,coldContext,true);if(closed.ImageHits!=0||closed.Fingerprint!=cold.Fingerprint||ownerMemo.Count!=0)throw new Exception("Closed owner fallback");parity++;
if(store.ReceivedCalls().Any()||trust.ReceivedCalls().Any())throw new Exception("Borrowing or prechecks invoked authority state/trust");
object Attr(Assembly assembly){var d=assembly.GetCustomAttribute<DebuggableAttribute>()!;return new{assembly.FullName,assembly.Location,sha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))),configuration=assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,flags=d.DebuggingFlags.ToString(),d.IsJITOptimizerDisabled,d.IsJITTrackingEnabled};}
File.WriteAllText(args[0],JsonSerializer.Serialize(new{diagnosticOnly=true,hostsStarted=false,authorityMethodsInvoked=false,storeAndTrustCalls=0,sharedActualAuthorityMemo=true,finalCompiledReceiverAttachmentAndBothActualBorrowHelpers=true,privateMemoCount=warmCount,privateMemoChargedBytes=warmBytes,
    parityChecks=parity,parityRows,methodName,actor=actor.Type.ActorTypeName,runtime=Environment.Version.ToString(),processorCount=Environment.ProcessorCount,serverGc=GCSettings.IsServerGC,
    sdk=Attr(typeof(RouteCatalogAuthority).Assembly),server=Attr(typeof(DevelopmentNativeActorReceiver).Assembly),probe=Attr(Assembly.GetExecutingAssembly()),cold=coldResult,
    fullFreshImageObservationsPerMethodRequest=20,regionsPerMethodRequest=2,rows},new JsonSerializerOptions{WriteIndented=true}));
