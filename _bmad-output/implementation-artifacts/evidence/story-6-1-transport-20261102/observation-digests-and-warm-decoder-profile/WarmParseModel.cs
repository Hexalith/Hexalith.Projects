using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
byte[] bytes=File.ReadAllBytes(args[0]);var options=(JsonSerializerOptions)typeof(RouteCatalogCodec).GetField("Options",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;var read=typeof(RouteCatalogCodec).GetMethod("ReadClosedDocument",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(RouteCatalogEnvelope));
var results=new List<object>();
for(int repeat=1;repeat<=3;repeat++)foreach(bool direct in new[]{false,true}){
 RouteCatalogEnvelope Run(){byte[] captured=bytes.ToArray();_=SHA256.HashData(captured);if(!captured.AsSpan().SequenceEqual(bytes))throw new Exception();return direct?JsonSerializer.Deserialize<RouteCatalogEnvelope>(captured,options)!:(RouteCatalogEnvelope)read.Invoke(null,[(ReadOnlyMemory<byte>)captured])!;}
 for(int i=0;i<12;i++)Run();long allocated=GC.GetAllocatedBytesForCurrentThread();var times=new List<double>();RouteCatalogEnvelope result=null!;for(int i=0;i<80;i++){long start=Stopwatch.GetTimestamp();result=Run();times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;times.Sort();if(!RouteCatalogCodec.Encode(result).AsSpan().SequenceEqual(bytes))throw new Exception("Exact roundtrip failed");results.Add(new{Repeat=repeat,Direct=direct,MedianMilliseconds=times[40],AverageAllocatedBytes=allocated/80,ExactCanonicalBytes=true});}
Console.WriteLine(JsonSerializer.Serialize(results));
