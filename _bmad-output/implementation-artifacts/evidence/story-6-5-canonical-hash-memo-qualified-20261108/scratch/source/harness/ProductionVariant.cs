using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Inventory = Hexalith.EventStore.ServiceDefaults.Routing.ObservedRouteCatalogInventory;
using Host = Hexalith.EventStore.ServiceDefaults.Routing.ObservedRouteCatalogHost;
namespace Story65.Canonical.Harness;
/// <summary>Production complete inventory and identity path, with the same original byte-memo scope ownership.</summary>
internal sealed class ProductionVariant : IVariant
{
    private readonly RouteCatalogValidatedBytesMemo _bytes = new();
    public string Name => "production";
    public IDisposable Enter() => Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope.EnterWithMemo(_bytes);
    public object Capture(RouteCatalogHostObservation observation, string workload)
        => workload == "identity" ? Host.Identity(observation) : new Inventory(observation).Observation;
    public string[] Digests(RouteCatalogHostObservation observation)
    {
        var inventory = new Inventory(observation);
        var owned = inventory.Observation;
        return [.. owned.AdmissionPolicies!.Select(inventory.Admission), .. owned.Declarations.Select(inventory.Declaration),
            .. (owned.Operations ?? []).Select(inventory.Operation)];
    }
    public object Counters() => new { ByteMemoEntries = _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes };
    public void Dispose() { _bytes.Dispose(); }
}
