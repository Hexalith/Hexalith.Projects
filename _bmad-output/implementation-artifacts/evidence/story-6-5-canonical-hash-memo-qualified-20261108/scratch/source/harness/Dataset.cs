using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>A public compiled-owner or checked-in fixture inventory; not a live complete host inventory.</summary>
internal sealed record Dataset(string Name, RouteCatalogHostObservation Observation)
{
    internal RouteCatalogHostObservation Fresh() => Observation with
    {
        Declarations = Observation.Declarations.ToArray(), Targets = Observation.Targets.ToArray(),
        AdmissionPolicies = Observation.AdmissionPolicies!.ToArray(), Operations = Observation.Operations?.ToArray(),
        RequiredHostIds = Observation.RequiredHostIds.ToArray(), TopologyDocuments = Observation.TopologyDocuments.Select(bytes => bytes.ToArray()).ToArray(),
        AuthenticationProfile = Observation.AuthenticationProfile.ToArray(),
    };
}
