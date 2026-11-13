using System.Reflection;using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Mvc;using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.Options;
internal sealed class GrantWorker : IDisposable
{
    private static readonly ConcreteGrants Actual=typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("ConcreteGrants",BindingFlags.Instance|BindingFlags.NonPublic)!.CreateDelegate<ConcreteGrants>();
    internal readonly GrantConfiguration Configuration;
    private readonly ServiceProvider _services;private readonly TechnicalHttpRouteCatalogReceiver _receiver=new();
    private readonly PlainGrantObservation _copy;private readonly FreshGrantObservation _fresh;
    internal GrantWorker(string json)
    {
        Configuration=new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{GrantConfiguration.Key,json}}).Build());
        _services=new ServiceCollection().AddSingleton<IConfiguration>(Configuration).AddSingleton<IOptions<JsonOptions>>(Options.Create(new JsonOptions())).BuildServiceProvider();
        typeof(TechnicalHttpRouteCatalogReceiver).GetField("_services",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(_receiver,_services);
        _copy=new(_services);_fresh=new(_services);
    }
    internal (string Output,int Reads,int RetainedCount,long RetainedBytes) Run(string variant,IReadOnlyList<RouteCatalogTechnicalHttpEndpoint> endpoints,bool capture)
    {
        Configuration.Reads=0;var pass=new TechnicalHttpObservationPass(_services);
        List<IReadOnlyList<RouteCatalogHttpGrant>>? all=capture?[]:null;int total=0;
        foreach(var endpoint in endpoints)
        {
            var grants=variant switch{"actual"=>Actual(_receiver,endpoint,pass),"copy"=>_copy.Grants(endpoint,pass),"fresh"=>_fresh.Grants(endpoint,pass),_=>throw new ArgumentException(variant)};
            total+=grants.Count;all?.Add(grants);
        }
        pass.Complete();
        var parsed=(TechnicalHttpParsedTemplate?)typeof(TechnicalHttpObservationPass).GetField("_parsedTemplate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(pass);
        if(parsed is not null&&(parsed.Count>1||parsed.RetainedBytes>1024*1024))throw new Exception("Final compiled parse storage exceeded bound");
        return(capture?JsonSerializer.Serialize(all):total.ToString(System.Globalization.CultureInfo.InvariantCulture),Configuration.Reads,parsed?.Count??0,parsed?.RetainedBytes??0);
    }
    public void Dispose()=>_services.Dispose();
}
