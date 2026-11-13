using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Inventory = Story65.Canonical.Candidate.ObservedRouteCatalogInventory;
using Host = Story65.Canonical.Candidate.ObservedRouteCatalogHost;
namespace Story65.Canonical.Harness;
/// <summary>Candidate complete inventory and identity path, with the same original byte-memo scope ownership.</summary>
internal sealed class CandidateVariant : IVariant
{
    private readonly RouteCatalogValidatedBytesMemo _bytes = new();
    private readonly Story65.Canonical.Candidate.RouteCatalogCanonicalHashMemo _hashes = new();
    public string Name => "candidate";
    public IDisposable Enter() => Story65.Canonical.Candidate.RouteCatalogDecodeScope.EnterWithCanonicalHashMemo(_bytes, _hashes);
    public object Capture(RouteCatalogHostObservation observation, string workload)
        => workload == "identity" ? Host.Identity(observation) : new Inventory(observation).Observation;
    public string[] Digests(RouteCatalogHostObservation observation)
    {
        var inventory = new Inventory(observation);
        var owned = inventory.Observation;
        return [.. owned.AdmissionPolicies!.Select(inventory.Admission), .. owned.Declarations.Select(inventory.Declaration),
            .. (owned.Operations ?? []).Select(inventory.Operation)];
    }
    public object Counters() => new { HashEntries = _hashes.Count, _hashes.RetainedCharge, _hashes.Hits, _hashes.Misses, ByteMemoEntries = _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes };
    public void Dispose() { _hashes.Dispose(); _bytes.Dispose(); }
}
