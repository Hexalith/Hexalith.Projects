using System.Reflection;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.Extensions.Options;

using Inventory = Hexalith.EventStore.ServiceDefaults.Routing.ObservedRouteCatalogInventory;
using Host = Hexalith.EventStore.ServiceDefaults.Routing.ObservedRouteCatalogHost;

namespace Story65.Canonical.Harness;

/// <summary>Uses only newly compiled real code and the actual authority-selected private owner scope.</summary>
internal sealed class ProductionVariant : IVariant
{
    private readonly RouteCatalogAuthority _authority;
    private readonly RouteCatalogValidatedBytesMemo _bytes;
    private readonly RouteCatalogCanonicalHashMemo _hashes;
    private readonly Func<RouteCatalogDecodeScope?> _enter;

    internal ProductionVariant()
    {
        var options = Options.Create(new RouteCatalogHostOptions
        {
            ScopedDecodeReuse = true,
            RetainValidatedCatalogBytes = true,
            DevelopmentFixture = true,
            RequireTechnicalHttpBinding = true,
            Profile = "Development",
        });
        // This scratch owner uses only BeginPrivateDecodeScope and Dispose. Their exact source
        // reads options/environment and owned memos only; Acquire/Validate/Release are never called.
        _authority = new RouteCatalogAuthority(null!, null!, options, environment: new BenchmarkHostEnvironment());
        _enter = typeof(RouteCatalogAuthority).GetMethod("BeginPrivateDecodeScope", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Func<RouteCatalogDecodeScope?>>(_authority);
        _bytes = (RouteCatalogValidatedBytesMemo)typeof(RouteCatalogAuthority)
            .GetField("_validatedBytes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_authority)!;
        _hashes = (RouteCatalogCanonicalHashMemo)typeof(RouteCatalogAuthority)
            .GetField("_canonicalHashes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_authority)!;
        using RouteCatalogDecodeScope? selected = _enter();
        Check.That(selected?.HasCanonicalHashMemo == true && selected.HasHostMemo, "actual-authority-selected-open-owner");
    }

    public string Name => "production";

    public IDisposable Enter()
    {
        return _enter() ?? throw new ClosedFailure("actual-authority-scope-required");
    }

    public object Capture(RouteCatalogHostObservation observation, string workload)
        => workload == "identity" ? Host.Identity(observation) : new Inventory(observation).Observation;

    public string[] Digests(RouteCatalogHostObservation observation)
    {
        var inventory = new Inventory(observation);
        var owned = inventory.Observation;
        return [.. owned.AdmissionPolicies!.Select(inventory.Admission), .. owned.Declarations.Select(inventory.Declaration),
            .. (owned.Operations ?? []).Select(inventory.Operation)];
    }

    internal void VerifyParticipation()
    {
        Check.That(_hashes.Count > 0 && _hashes.Hits > 0, "actual-compiled-hash-owner-participated");
    }

    public object Counters() => new
    {
        HashEntries = _hashes.Count, _hashes.RetainedCharge, _hashes.Hits, _hashes.Misses,
        ByteMemoEntries = _bytes.Count, ByteMemoRetained = _bytes.RetainedBytes,
        ActualAuthorityOwner = true, ActualSelectorOptionObservationsPerEntry = 3,
        OriginalOwnerMemos = 3, AdditionalCanonicalHashMemo = 1,
        ScopeDelegateBoundOutsideCapture = true, NoPerCaptureReflection = true,
    };

    public void Dispose() => _authority.Dispose();
}
