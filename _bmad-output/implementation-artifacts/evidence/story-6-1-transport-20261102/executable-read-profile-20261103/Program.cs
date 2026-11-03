using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.DomainService;
string root="/home/administrator/projects/hexalith/projects";
Assembly owner=Assembly.LoadFrom(root+"/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.dll");
Assembly[] implementations=[owner,typeof(IDomainQueryHandler).Assembly,..owner.GetReferencedAssemblies().Select(Assembly.Load)];
Assembly[] assemblies=implementations.Distinct().OrderBy(a=>a.FullName,StringComparer.Ordinal).ToArray();
var files=assemblies.Select(a=>new {a.FullName,path=a.Location,bytes=new FileInfo(a.Location).Length}).ToArray();
List<object> trials=[];Dictionary<string,string>? expected=null;
for(int i=0;i<30;i++) {
 foreach(bool stream in i%2==0?new[]{false,true}:new[]{true,false}) {
  long before=GC.GetAllocatedBytesForCurrentThread();var sw=Stopwatch.StartNew();
  var result=assemblies.ToDictionary(a=>a.FullName!,a=> {
   if(!stream)return Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a.Location)));
   using FileStream image=File.OpenRead(a.Location);return Convert.ToHexStringLower(SHA256.HashData(image));
  },StringComparer.Ordinal);
  double ms=sw.Elapsed.TotalMilliseconds;long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
  expected??=result;
  if(!expected.OrderBy(v=>v.Key).SequenceEqual(result.OrderBy(v=>v.Key)))throw new InvalidOperationException("Fresh hashes differ.");
  trials.Add(new{trial=i,stream,elapsedMs=ms,allocatedBytes=allocated});
 }
}
Console.WriteLine(JsonSerializer.Serialize(new{diagnosticOnly=true,nativeRuntimeEvidence=false,independentlyFreshFileReadsEveryTrial=true,allDigestsEqual=true,totalBytes=files.Sum(f=>f.bytes),files,trials},new JsonSerializerOptions{WriteIndented=true}));
