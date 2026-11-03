using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);byte[] activationBytes=File.ReadAllBytes(args[0]),observationBytes=File.ReadAllBytes(args[1]);
var activation=JsonSerializer.Deserialize<RouteCatalogActivation>(activationBytes,options)!;
var sample=JsonSerializer.Deserialize<RouteCatalogHostObservation>(observationBytes,options)!;var expected=ObservedRouteCatalogHost.Identity(sample);
var enter=typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!.GetMethod("Enter",BindingFlags.Static|BindingFlags.NonPublic)!;
var observer=new ObservedRouteCatalogHost(_=>Task.FromResult(JsonSerializer.Deserialize<RouteCatalogHostObservation>(observationBytes,options)!));
for(int warm=0;warm<8;warm++){using var scope=(IDisposable)enter.Invoke(null,null)!;RouteCatalogCodec.Decode(activation.CanonicalCatalog,activation.RootDigest);await observer.ObserveAsync(activation,sample.AuthorityCoordinates!,default);}
var times=new List<double>();long allocations=GC.GetTotalAllocatedBytes(true);RouteCatalogHostIdentity? actual=null;
for(int index=0;index<30;index++){
 using var scope=(IDisposable)enter.Invoke(null,null)!;RouteCatalogCodec.Decode(activation.CanonicalCatalog,activation.RootDigest);
 long started=Stopwatch.GetTimestamp();actual=await observer.ObserveAsync(activation,sample.AuthorityCoordinates!,default);times.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
 if(actual!=expected)throw new InvalidOperationException("Fresh identity differs");
}
allocations=GC.GetTotalAllocatedBytes(true)-allocations;times.Sort();
Console.WriteLine(JsonSerializer.Serialize(new{Owner=sample.AppId,Iterations=30,AuxiliaryInstalled=sample.AuxiliaryOperations!.Count,AuxiliaryRequired=(activation.AuxiliaryOperations??[]).Count(x=>x.AppId==sample.AppId),MedianMilliseconds=times[15],P95Milliseconds=times[28],AverageAllocatedBytes=allocations/30,IdentityMatches=true,IdentitySha256=Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(actual))),ActivationInputSha256=Convert.ToHexStringLower(SHA256.HashData(activationBytes)),ObservationInputSha256=Convert.ToHexStringLower(SHA256.HashData(observationBytes)),AuthorityOrRuntimeCallsPerformed=false}));
