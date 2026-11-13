using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Inventory = Story65.Canonical.Control.ObservedRouteCatalogInventory;
using Host = Story65.Canonical.Control.ObservedRouteCatalogHost;
namespace Story65.Canonical.Harness;
/// <summary>Control complete inventory and identity path, with the same original byte-memo scope ownership.</summary>
internal sealed class ControlVariant : IVariant
{
    private readonly RouteCatalogValidatedBytesMemo _bytes = new();
    public string Name => "control";
    public IDisposable Enter() => Story65.Canonical.Control.RouteCatalogDecodeScope.EnterWithMemo(_bytes);
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
