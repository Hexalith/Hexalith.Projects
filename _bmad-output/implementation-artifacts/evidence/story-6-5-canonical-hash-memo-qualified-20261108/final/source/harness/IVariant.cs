using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>Matches one per-authority owner and one fresh closing scope per observation.</summary>
internal interface IVariant : IDisposable
{
    string Name { get; }
    IDisposable Enter();
    object Capture(RouteCatalogHostObservation observation, string workload);
    string[] Digests(RouteCatalogHostObservation observation);
    object Counters();
}
