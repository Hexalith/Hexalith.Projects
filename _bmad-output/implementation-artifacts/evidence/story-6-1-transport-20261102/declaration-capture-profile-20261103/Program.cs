using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Contracts.Routing;
string root = "/home/administrator/projects/hexalith/projects";
string[] paths = [root+"/src/Hexalith.Projects.Server/bin/Debug/net10.0/Hexalith.Projects.Server.dll",root+"/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.dll",root+"/references/Hexalith.Tenants/src/Hexalith.Tenants.Server/bin/Debug/net10.0/Hexalith.Tenants.Contracts.dll"];
Assembly[] owners = paths.Select(Assembly.LoadFrom).ToArray();
List<object> trials=[];
for(int i=0;i<25;i++) {
 long before=GC.GetAllocatedBytesForCurrentThread();var sw=Stopwatch.StartNew();
 var first=CompiledRouteCatalogDeclarations.Read(owners);double once=sw.Elapsed.TotalMilliseconds;
 var second=CompiledRouteCatalogDeclarations.Read(owners);var third=CompiledRouteCatalogDeclarations.Read(owners);double three=sw.Elapsed.TotalMilliseconds;
 if(!first.SequenceEqual(second)||!first.SequenceEqual(third)) {
  if(!first.Select(RouteCatalogCodec.ComputeDeclarationDigest).SequenceEqual(second.Select(RouteCatalogCodec.ComputeDeclarationDigest)) || !first.Select(RouteCatalogCodec.ComputeDeclarationDigest).SequenceEqual(third.Select(RouteCatalogCodec.ComputeDeclarationDigest))) throw new InvalidOperationException("Fresh declarations differ.");
 }
 long readBytes=GC.GetAllocatedBytesForCurrentThread()-before;sw.Restart();var ops=CompiledRouteCatalogDeclarations.ReadOperations(owners);
 trials.Add(new{trial=i,cold=i==0,onceMs=once,threeMs=three,avoidableTwoReadsMs=three-once,readThreeAllocatedBytes=readBytes,operationsMs=sw.Elapsed.TotalMilliseconds,declarationCount=first.Count,operationCount=ops.Count});
}
Console.WriteLine(JsonSerializer.Serialize(new{diagnosticOnly=true,nativeRuntimeEvidence=false,scope="Actual configured owner binaries; unchanged public compiled-declaration functions. No source change or authority cache.",binaries=paths.ToDictionary(p=>p,p=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)))),trials},new JsonSerializerOptions{WriteIndented=true}));
