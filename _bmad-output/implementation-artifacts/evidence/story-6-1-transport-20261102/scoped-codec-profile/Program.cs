using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
byte[] bytes=File.ReadAllBytes(args[0]);string root=Convert.ToHexStringLower(SHA256.HashData(bytes));
var enter=typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!.GetMethod("Enter",BindingFlags.Static|BindingFlags.NonPublic)!;
var results=new List<object>();
for(int repeat=1;repeat<=3;repeat++) foreach(bool scoped in new[]{false,true}) {
 for(int warm=0;warm<8;warm++){using IDisposable scope=scoped?(IDisposable)enter.Invoke(null,null)!:null;for(int read=0;read<7;read++)RouteCatalogCodec.Decode(bytes,root);}
 var samples=new List<double>();long allocated=GC.GetAllocatedBytesForCurrentThread();RouteCatalogEnvelope last=null;
 for(int iteration=0;iteration<30;iteration++) {
  long started=Stopwatch.GetTimestamp();using(IDisposable scope=scoped?(IDisposable)enter.Invoke(null,null)!:null)for(int read=0;read<7;read++)last=RouteCatalogCodec.Decode(bytes,root);
  samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
 }
 allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;samples.Sort();
 byte[] encoded=RouteCatalogCodec.Encode(last);if(!encoded.SequenceEqual(bytes))throw new InvalidOperationException("Canonical mismatch");
 results.Add(new{Repeat=repeat,Scoped=scoped,Iterations=30,DecodesPerAcquisition=7,CatalogBytes=bytes.Length,InputRootSha256=root,EncodedRootSha256=Convert.ToHexStringLower(SHA256.HashData(encoded)),last.RouteFacetDigest,last.IdempotencyFacetDigest,MinimumMilliseconds=samples[0],MedianMilliseconds=samples[15],P95Milliseconds=samples[28],AverageAllocatedBytes=allocated/30,CanonicalRoundTripExact=true});
}
Console.WriteLine(JsonSerializer.Serialize(results));
