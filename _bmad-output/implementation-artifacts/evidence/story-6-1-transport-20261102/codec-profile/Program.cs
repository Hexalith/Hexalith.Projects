using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
byte[] bytes = File.ReadAllBytes(args[0]);
string root = Convert.ToHexStringLower(SHA256.HashData(bytes));
for(int warm=0;warm<20;warm++) RouteCatalogCodec.Decode(bytes, root);
var samples = new List<double>();
long allocated=GC.GetAllocatedBytesForCurrentThread();
for(int index=0;index<100;index++) {
 long started=Stopwatch.GetTimestamp();
 var envelope=RouteCatalogCodec.Decode(bytes,root);
 if(index==0 && !RouteCatalogCodec.Encode(envelope).SequenceEqual(bytes)) throw new InvalidOperationException("Canonical mismatch");
 samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
}
allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
samples.Sort();
Console.WriteLine(JsonSerializer.Serialize(new { CatalogBytes=bytes.Length, RootSha256=root, Iterations=samples.Count, MinimumMilliseconds=samples[0], MedianMilliseconds=samples[50], P95Milliseconds=samples[95], MaximumMilliseconds=samples[^1], AverageAllocatedBytes=allocated/samples.Count, CanonicalRoundTripExact=true }));
