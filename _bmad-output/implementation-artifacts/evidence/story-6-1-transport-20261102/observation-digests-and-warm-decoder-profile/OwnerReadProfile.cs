using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
var dirs=args.Select(Path.GetFullPath).Select(Path.GetDirectoryName).ToArray();
AssemblyLoadContext.Default.Resolving+=(context,name)=>{foreach(var dir in dirs){string path=Path.Combine(dir!,name.Name+".dll");if(File.Exists(path))return context.LoadFromAssemblyPath(path);}return null;};
var owners=args.Select(path=>AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path))).ToArray();
var results=new List<object>();
foreach(string part in new[]{"one-read","three-reads","operations-with-read","current-four-reads-plus-operations"}){
 void Run(){if(part is "one-read" or "current-four-reads-plus-operations")CompiledRouteCatalogDeclarations.Read(owners);if(part is "three-reads" or "current-four-reads-plus-operations")for(int i=0;i<(part=="three-reads"?3:2);i++)CompiledRouteCatalogDeclarations.Read(owners);if(part is "operations-with-read" or "current-four-reads-plus-operations")CompiledRouteCatalogDeclarations.ReadOperations(owners);}
 for(int i=0;i<8;i++)Run();var times=new List<double>();long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<30;i++){long start=Stopwatch.GetTimestamp();Run();times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;times.Sort();results.Add(new{Part=part,MedianMilliseconds=times[15],AverageAllocatedBytes=allocated/30});
}
Console.WriteLine(JsonSerializer.Serialize(new{OperationsSha256=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(CompiledRouteCatalogDeclarations.ReadOperations(owners)))),Assemblies=owners.Select(x=>x.GetName().Name),RouteCount=CompiledRouteCatalogDeclarations.Read(owners).Count,OperationCount=CompiledRouteCatalogDeclarations.ReadOperations(owners).Count,Results=results}));
