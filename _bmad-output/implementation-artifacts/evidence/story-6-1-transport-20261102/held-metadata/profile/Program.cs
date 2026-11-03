using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

byte[] activationBytes=File.ReadAllBytes(args[0]), observationBytes=File.ReadAllBytes(args[1]);
var activation=JsonSerializer.Deserialize<RouteCatalogActivation>(activationBytes,JsonSerializerOptions.Web)!;
var observation=JsonSerializer.Deserialize<RouteCatalogHostObservation>(observationBytes,JsonSerializerOptions.Web)!;
var identity=ObservedRouteCatalogHost.Identity(observation);
var coordinates=observation.AuthorityCoordinates!;
var options=new RouteCatalogHostOptions { Identity=identity,Profile="Development",DevelopmentFixture=true,RequireTechnicalHttpBinding=true,
    ScopedDecodeReuse=true,RetainValidatedCatalogBytes=true,StateStore=coordinates.StateStore,StateKey=coordinates.StateKey };
var wrapped=Options.Create(options);
var environment=WebApplication.CreateSlimBuilder(new WebApplicationOptions {EnvironmentName=Environments.Development}).Environment;
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["DAPR_HTTP_ENDPOINT"]=coordinates.HttpEndpoint}).Build();
using var transport=new StateTransport(RouteCatalogActivationState.Empty with {Committed=activation,Epoch=1});
var store=new DaprRouteCatalogStateStore(new ProfileClients(transport),config,wrapped);
int observerCalls=0;
var observer=new ObservedRouteCatalogHost(_=>{observerCalls++;return Task.FromResult(JsonSerializer.Deserialize<RouteCatalogHostObservation>(observationBytes,JsonSerializerOptions.Web)!);});
using var authority=new RouteCatalogAuthority(store,new DevelopmentRouteCatalogTrustValidator(wrapped,environment),wrapped,observer,environment:environment);
var results=new List<object>();
foreach(bool reuse in new[]{false,true,true,false,false,true})
{
    options.ReuseHeldMetadataDispatch=reuse;
    await using var parent=await authority.AcquireAsync();
    using var receiver=RouteCatalogRemoteCall.EnterReceiver(parent,CancellationToken.None);
    async Task RunAsync()
    {
        if(!reuse){await using var child=await authority.AcquireAsync();return;}
        using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(new Uri(coordinates.HttpEndpoint),"v1.0/invoke/eventstore/method/api/v1/streams/read/workload"))
        {Content=JsonContent.Create(new StreamReadRequest("profile-tenant","folders","profile-folder",0,0,PageSize:1){RequireNativeProof=true})};
        using var held=await authority.GetHeldMetadataDispatchAsync(request,"eventstore","api/v1/streams/read/workload",default);
        if(held is null || !ReferenceEquals(held.Lease,parent) || held.Snapshot.RootDigest!=activation.RootDigest)throw new Exception("Real held validation not selected");
    }
    for(int i=0;i<6;i++)await RunAsync();
    int reads=transport.Reads,writes=transport.Writes,observed=observerCalls;
    List<double> times=[];long allocated=GC.GetTotalAllocatedBytes(true);
    for(int i=0;i<30;i++){long start=Stopwatch.GetTimestamp();await RunAsync();times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}
    allocated=GC.GetTotalAllocatedBytes(true)-allocated;times.Sort();
    if(observerCalls-observed!=30)throw new Exception("Fresh host observation missing");
    if(reuse && (transport.Reads!=reads || transport.Writes!=writes))throw new Exception("Held scope unexpectedly reacquired state/fence");
    results.Add(new{mode=reuse?"held-with-fresh-validation-and-owned-body":"ordinary-fresh-acquire-and-release",operations=30,medianMs=times[15],p95Ms=times[28],allocatedBytesPerOperation=allocated/30,stateReads=transport.Reads-reads,stateWrites=transport.Writes-writes,freshHostObservations=observerCalls-observed});
}
var lifetimeResults=new List<object>();
foreach(bool reuse in new[]{false,true,true,false,false,true})
{
    options.ReuseHeldMetadataDispatch=reuse;
    async Task LifetimeAsync()
    {
        await using var parent=await authority.AcquireAsync();
        using var receiver=RouteCatalogRemoteCall.EnterReceiver(parent,CancellationToken.None);
        if(!reuse){await using var child=await authority.AcquireAsync();return;}
        using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(new Uri(coordinates.HttpEndpoint),"v1.0/invoke/eventstore/method/api/v1/streams/read/workload"))
        {Content=JsonContent.Create(new StreamReadRequest("profile-tenant","folders","profile-folder",0,0,PageSize:1){RequireNativeProof=true})};
        using var held=await authority.GetHeldMetadataDispatchAsync(request,"eventstore","api/v1/streams/read/workload",default);
        if(held is null || !ReferenceEquals(held.Lease,parent) || held.Snapshot.RootDigest!=activation.RootDigest)throw new Exception("Real held lifetime not selected");
    }
    for(int i=0;i<6;i++)await LifetimeAsync();
    int reads=transport.Reads,writes=transport.Writes,observed=observerCalls;
    List<double> times=[];long allocated=GC.GetTotalAllocatedBytes(true);
    for(int i=0;i<30;i++){long start=Stopwatch.GetTimestamp();await LifetimeAsync();times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}
    allocated=GC.GetTotalAllocatedBytes(true)-allocated;times.Sort();
    if(observerCalls-observed!=60)throw new Exception("Parent and send fresh host observations missing");
    lifetimeResults.Add(new{mode=reuse?"parent-acquire-held-validation-parent-release":"parent-acquire-child-acquire-child-release-parent-release",operations=30,medianMs=times[15],p95Ms=times[28],allocatedBytesPerOperation=allocated/30,stateReads=transport.Reads-reads,stateWrites=transport.Writes-writes,freshHostObservations=observerCalls-observed});
}
string Hash(byte[] bytes)=>Convert.ToHexStringLower(SHA256.HashData(bytes));
Console.WriteLine(JsonSerializer.Serialize(new{actualCatalogBytes=activation.CanonicalCatalog.Length,canonicalRoot=Hash(activation.CanonicalCatalog),rootMatches=Hash(activation.CanonicalCatalog)==activation.RootDigest,
    activationInputSha256=Hash(activationBytes),observationInputSha256=Hash(observationBytes),observationIdentitySha256=Hash(JsonSerializer.SerializeToUtf8Bytes(identity)),configuration="ordinary Debug / Optimize=false / DEBUG enabled",
    authorityStateTransport="owned in-memory HTTP model through actual DaprRouteCatalogStateStore; no network or live authority evidence",independentReceiversAndRemoteRpcNotMeasured=true,
    results,lifetimeResults},new JsonSerializerOptions{WriteIndented=true}));
