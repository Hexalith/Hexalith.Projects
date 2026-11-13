using System.Diagnostics;using System.Reflection;using System.Runtime;using System.Security.Cryptography;using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;using Hexalith.EventStore.ServiceDefaults.Routing;
var profiles=await QualifiedProfiles.CreateAsync();var variants=new[]{"fresh","copy","actual"};int parity=0;
var controls=new List<object>();
foreach(var profile in profiles)
{
    using var worker=new GrantWorker(profile.Json);var expected=worker.Run("fresh",profile.Endpoints,true);
    foreach(string variant in variants){var observed=worker.Run(variant,profile.Endpoints,true);if(observed.Output!=expected.Output||observed.Reads!=profile.Endpoints.Length)throw new Exception("Final wholeConcreteGrants output/read parity");parity++;controls.Add(new{profile=profile.Name,variant,observed.Reads,observed.RetainedCount,observed.RetainedBytes});}
    RouteCatalogTechnicalHttpEndpoint Endpoint(string text)=>new(text,["GET","POST"],"","","",[],"","");
    var repeated=new[]{Endpoint("/owned/{id:int}"),Endpoint("/owned/{id:int}"),Endpoint("/owned/{id:int}")};
    string Json(string path,string verb)=>JsonSerializer.Serialize(new[]{new RouteCatalogDeclaredInvocation("caller",path,[verb])});
    worker.Configuration.Current=read=>read switch{1=>Json("owned/7","GET"),2=>Json("owned/bad","GET"),_=>Json("owned/8","POST")};
    expected=worker.Run("fresh",repeated,true);
    foreach(string variant in variants){var observed=worker.Run(variant,repeated,true);if(observed.Output!=expected.Output||observed.Reads!=3)throw new Exception("Final configuration/verb drift");parity++;}
    foreach(string json in new[]{"[]","[malformed",Json("owned/7","GET")})
    {
        worker.Configuration.Current=_=>json;var malformed=new[]{Endpoint("/owned/{invalid")};
        string Result(string variant){try{return worker.Run(variant,malformed,true).Output;}catch(Exception e){return e.GetType().FullName+"|"+e.Message;}}
        string original=Result("fresh");foreach(string variant in variants){if(Result(variant)!=original)throw new Exception("Final malformed/config precedence");parity++;}
    }
}
var first=new List<object>();
foreach(var profile in profiles)foreach(string variant in variants)
{
    using var worker=new GrantWorker(profile.Json);long bytes=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();var value=worker.Run(variant,profile.Endpoints,false);
    first.Add(new{profile=profile.Name,variant,elapsedMs=Stopwatch.GetElapsedTime(began).TotalMilliseconds,allocatedBytes=GC.GetAllocatedBytesForCurrentThread()-bytes,value.Reads,value.RetainedCount,value.RetainedBytes});
}
object Cohort(GrantCorpus profile,int workers,string variant,int iterations)
{
    using var start=new Barrier(workers+1);var times=new double[workers];var bytes=new long[workers];var reads=new long[workers];Exception? failure=null;
    var threads=Enumerable.Range(0,workers).Select(index=>new Thread(()=>
    {
        try
        {
            using var worker=new GrantWorker(profile.Json);start.SignalAndWait();long b=GC.GetAllocatedBytesForCurrentThread(),t=Stopwatch.GetTimestamp();
            for(int i=0;i<iterations;i++){var value=worker.Run(variant,profile.Endpoints,false);if(value.Reads!=profile.Endpoints.Length)throw new Exception("Final physical reads skipped");reads[index]+=value.Reads;}
            times[index]=Stopwatch.GetElapsedTime(t).TotalMilliseconds;bytes[index]=GC.GetAllocatedBytesForCurrentThread()-b;
        }
        catch(Exception e){Interlocked.CompareExchange(ref failure,e,null);}
    }){IsBackground=true}).ToArray();
    foreach(var thread in threads)thread.Start();long all=GC.GetTotalAllocatedBytes(true),cpu=Process.GetCurrentProcess().TotalProcessorTime.Ticks,began=Stopwatch.GetTimestamp();int[] gc=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();
    start.SignalAndWait();foreach(var thread in threads)thread.Join();if(failure is not null)throw failure;
    return new{profile=profile.Name,workers,variant,iterationsPerWorker=iterations,passes=workers*iterations,meanPassMs=times.Average()/iterations,elapsedMs=Stopwatch.GetElapsedTime(began).TotalMilliseconds,allocatedBytesPerPass=bytes.Sum()/(workers*iterations),processAllocatedBytes=GC.GetTotalAllocatedBytes(true)-all,processCpuMs=(Process.GetCurrentProcess().TotalProcessorTime.Ticks-cpu)/10000d,reads=reads.Sum(),gcDelta=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-gc[i]).ToArray()};
}
var rows=new List<object>();
foreach(var profile in profiles)foreach(int workers in new[]{2,4})
{
    foreach(string variant in variants)Cohort(profile,workers,variant,24);
    for(int sample=0;sample<7;sample++)foreach(string variant in variants.Skip(sample%3).Concat(variants.Take(sample%3)))rows.Add(new{sample,result=Cohort(profile,workers,variant,12)});
}
object Attr(Assembly a){var d=a.GetCustomAttribute<DebuggableAttribute>()!;return new{a.FullName,a.Location,sha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a.Location))),configuration=a.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,flags=d.DebuggingFlags.ToString(),d.IsJITOptimizerDisabled,d.IsJITTrackingEnabled};}
File.WriteAllText(args[0],JsonSerializer.Serialize(new{diagnosticOnly=true,finalCompiledImplementation=true,ownerBuildsByProbe=false,hostsStarted=false,featureAcceptance=false,deadlineGainClaimed=false,parityChecks=parity,controls,runtime=Environment.Version.ToString(),processorCount=Environment.ProcessorCount,serverGc=GCSettings.IsServerGC,oracle=Attr(typeof(TechnicalHttpRouteTemplate).Assembly),probe=Attr(Assembly.GetExecutingAssembly()),profiles=profiles.Select(p=>new{p.Name,endpointCount=p.Endpoints.Length,invocationCount=p.Invocations.Length,p.Endpoints,p.Invocations,p.Refused}),firstTimedAfterParity=first,rows},new JsonSerializerOptions{WriteIndented=true}));
