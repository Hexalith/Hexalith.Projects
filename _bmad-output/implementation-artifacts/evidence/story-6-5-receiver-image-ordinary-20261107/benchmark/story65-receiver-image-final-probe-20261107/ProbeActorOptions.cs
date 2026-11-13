using Dapr.Actors.Runtime;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.Options;
internal sealed class ProbeActorOptions(IOptions<ActorRuntimeOptions> inner) : IOptions<ActorRuntimeOptions>
{
    private readonly ThreadLocal<List<RouteCatalogImageDigestScope?>> _observed=new(()=>[]);
    public ActorRuntimeOptions Value { get { _observed.Value!.Add(RouteCatalogImageDigestScope.Current);return inner.Value; } }
    internal void Reset()=>_observed.Value!.Clear();
    internal int Hits()=>_observed.Value!.Where(scope=>scope is not null).Distinct(ReferenceEqualityComparer.Instance).Cast<RouteCatalogImageDigestScope>().Sum(scope=>scope.CacheHits);
}
