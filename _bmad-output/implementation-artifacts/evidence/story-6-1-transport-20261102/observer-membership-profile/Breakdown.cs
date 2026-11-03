using System.Diagnostics;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
byte[] bytes=File.ReadAllBytes(args[0]);var input=JsonSerializer.Deserialize<RouteCatalogHostObservation>(bytes,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var results=new List<object>();
foreach(string part in new[]{"identity","admission-digests","declaration-digests","operation-digests"}){
 void Run(){switch(part){case "identity":_ = ObservedRouteCatalogHost.Identity(input);break;case "admission-digests":foreach(var x in input.AdmissionPolicies!)_ =RouteCatalogCodec.ComputeIdempotencyPolicyDigest(x);break;case "declaration-digests":foreach(var x in input.Declarations)_ =RouteCatalogCodec.ComputeDeclarationDigest(x);break;case "operation-digests":foreach(var x in input.Operations!)_ =RouteCatalogCodec.ComputeOperationContractDigest(x);break;}}
 for(int i=0;i<15;i++)Run();var times=new List<double>();long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<50;i++){long start=Stopwatch.GetTimestamp();Run();times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;times.Sort();results.Add(new{Part=part,MedianMilliseconds=times[25],AverageAllocatedBytes=allocated/50});
}
Console.WriteLine(JsonSerializer.Serialize(results));
