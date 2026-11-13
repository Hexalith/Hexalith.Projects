using Hexalith.EventStore.Contracts.Routing;
namespace Story65.TechnicalFingerprint.Harness;
internal interface ITechnicalHost : IDisposable
{
    string Name { get; }
    IObservation BeginObservation();
    RouteCatalogTechnicalHttpEndpoint[] Observe(MvcCorpus corpus);
    MemoCounters Counters { get; }
}
