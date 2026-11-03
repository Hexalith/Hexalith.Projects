using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
byte[] activation = File.ReadAllBytes(args[0]), observation = File.ReadAllBytes(args[1]);
var json = JsonSerializerOptions.Web;
var original = JsonSerializer.Deserialize<RouteCatalogActivation>(activation, json)!;
var facts = JsonSerializer.Deserialize<RouteCatalogHostObservation>(observation, json)!;
var expected = original.RequiredHosts.Single(host => host.AppId == facts.AppId);
var scopeType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope")!;
var memoType = typeof(RouteCatalogCodec).Assembly.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogValidatedBytesMemo")!;
var enterMemo = scopeType.GetMethod("EnterWithMemo", BindingFlags.Static | BindingFlags.NonPublic)!;
using var memo = (IDisposable)Activator.CreateInstance(memoType, nonPublic:true)!;
using(var seed=(IDisposable)enterMemo.Invoke(null,[memo])!) RouteCatalogCodec.Decode(original.CanonicalCatalog, original.RootDigest);
for(int i=0;i<12;i++) {
 ProfileTimings.Warmup=i<3;
 for(int n=0;n<10;n++) {
  var candidate=JsonSerializer.Deserialize<RouteCatalogActivation>(activation,json)!;
  var host=new ProfileObserver(_ => Task.FromResult(JsonSerializer.Deserialize<RouteCatalogHostObservation>(observation,json)!));
  using var scope=(IDisposable)enterMemo.Invoke(null,[memo])!;
  var actual=await host.ObserveAsync(candidate,facts.AuthorityCoordinates!,CancellationToken.None);
  if(actual!=expected) throw new InvalidOperationException("Actual captured observation differs.");
 }
}
Console.WriteLine(JsonSerializer.Serialize(ProfileTimings.Values,new JsonSerializerOptions{WriteIndented=true}));
public static class ProfileTimings {
 public static bool Warmup; public static Dictionary<string,List<double>> Values=new();
 public static void Record(string phase,double ms) {if(Warmup)return;if(!Values.TryGetValue(phase,out var values))Values[phase]=values=[];values.Add(ms);}
}
