using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Story65.Canonical.Control;

/// <summary>Exact extracted complete Identity methods; excludes ObserveAsync and its decision memo.</summary>
internal static class ObservedRouteCatalogHost
{
    /// <summary>Calculates the observed host identity; no candidate-supplied fingerprint participates in a preimage.</summary>
    public static RouteCatalogHostIdentity Identity(RouteCatalogHostObservation observation) => Identity(observation, out _);

    private static RouteCatalogHostIdentity Identity(RouteCatalogHostObservation observation, out ObservedRouteCatalogInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (string.IsNullOrWhiteSpace(observation.HostId) || string.IsNullOrWhiteSpace(observation.AppId)
            || observation.Declarations is null || observation.Targets is null || observation.RequiredHostIds is not { Count: > 0 }
            || observation.TopologyDocuments is not { Count: > 0 } || observation.TopologyDocuments.Any(d => d is not { Length: > 0 })
            || observation.AuthenticationProfile is not { Length: > 0 }
            || observation.AuthorityCoordinates is not { StateStore.Length: > 0, StateKey.Length: > 0, HttpEndpoint.Length: > 0 }
            || observation.AdmissionPolicies is null || observation.ProjectionCapabilities is null || observation.AuxiliaryOperations is null
            || observation.ProjectionCapabilities.Any(c => c.AppId != observation.AppId || string.IsNullOrWhiteSpace(c.Fingerprint)
                || c.ProjectionTypes is not { Count: > 0 } || c.ProjectionTypes.Distinct(StringComparer.Ordinal).Count() != c.ProjectionTypes.Count)
            || observation.ProjectionCapabilities.Select(c => (c.Domain, c.ContractVersion)).Distinct().Count() != observation.ProjectionCapabilities.Count
            || observation.AuxiliaryOperations.Any(a => a.AppId != observation.AppId || string.IsNullOrWhiteSpace(a.Operation))
            || observation.AuxiliaryOperations.Select(a => (a.Domain, a.Method)).Distinct().Count() != observation.AuxiliaryOperations.Count)
            throw new RouteCatalogUnavailableException("Independent host inventory, loaded topology and authentication observations are required.", RouteCatalogRefusalReason.Configuration);
        inventory = new(observation);
        observation = inventory.Observation;
        string inventoryDigest = Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            admission = observation.AdmissionPolicies!.OrderBy(a => a.RouteId, StringComparer.Ordinal).Select(inventory.Admission).ToArray(),
            capabilities = observation.ProjectionCapabilities!.OrderBy(c => c.Domain, StringComparer.Ordinal).ThenBy(c => c.ContractVersion, StringComparer.Ordinal)
                .Select(c => new { c.AppId, c.Domain, c.ContractVersion, c.Fingerprint, types = c.ProjectionTypes.Order(StringComparer.Ordinal).ToArray() }).ToArray(),
            auxiliary = observation.AuxiliaryOperations!.OrderBy(a => a.Domain, StringComparer.Ordinal).ThenBy(a => a.Method, StringComparer.Ordinal).ToArray(),
            declarations = observation.Declarations.OrderBy(d => d.RouteId, StringComparer.Ordinal).Select(inventory.Declaration).ToArray(),
            operations = observation.Operations?.OrderBy(o => o.RouteId, StringComparer.Ordinal).Select(inventory.Operation).ToArray(),
            targets = observation.Targets.OrderBy(t => t.RouteId, StringComparer.Ordinal).Select(t => new
            { t.RouteId, t.DeclarationDigest, t.AppId, t.Method, t.ContractVersion, t.CredentialKind, t.Operation, t.IsRuntimeOverride, t.ResolverTenantPattern }).ToArray(),
        }));
        string topology = Hash(JsonSerializer.SerializeToUtf8Bytes(new
        {
            hosts = observation.RequiredHostIds.Order(StringComparer.Ordinal).ToArray(),
            documents = observation.TopologyDocuments.Select(Hash).ToArray(),
            authority = observation.AuthorityCoordinates,
            receiver = observation.ReceiverSurface,
        }));
        if (observation.SelectedAppIds is not null || observation.DeclaredInvocations is not null)
        {
            if (observation.SelectedAppIds is null || observation.DeclaredInvocations is null
                || !observation.SelectedAppIds.Contains(observation.AppId, StringComparer.Ordinal)
                || observation.SelectedAppIds.Any(string.IsNullOrWhiteSpace)
                || observation.SelectedAppIds.Distinct(StringComparer.Ordinal).Count() != observation.SelectedAppIds.Count
                || observation.DeclaredInvocations.Any(invocation => string.IsNullOrWhiteSpace(invocation.CallerAppId)
                    || string.IsNullOrWhiteSpace(invocation.Method) || invocation.HttpMethods is not { Count: > 0 }))
                throw new RouteCatalogUnavailableException("An exact selected application and declared caller inventory is required.", RouteCatalogRefusalReason.Configuration);
            topology = Hash(JsonSerializer.SerializeToUtf8Bytes(new
            {
                topology, selectedApps = observation.SelectedAppIds.Order(StringComparer.Ordinal).ToArray(),
                declaredInvocations = observation.DeclaredInvocations.OrderBy(invocation => invocation.CallerAppId, StringComparer.Ordinal)
                    .ThenBy(invocation => invocation.Method, StringComparer.Ordinal).ToArray(),
            }));
        }
        string? executable = null;
        if (observation.ExecutableOwners is { } owners)
        {
            if (owners.Count == 0 || owners.Any(owner => string.IsNullOrWhiteSpace(owner.Key)
                || owner.Value is not { Length: 64 } || !owner.Value.All(char.IsAsciiHexDigitLower)))
                throw new RouteCatalogUnavailableException("The actual executable owner identities are incomplete.", RouteCatalogRefusalReason.Inventory);
            executable = Hash(JsonSerializer.SerializeToUtf8Bytes(owners.OrderBy(owner => owner.Key, StringComparer.Ordinal).ToArray()));
        }
        return new(observation.HostId, observation.AppId, inventoryDigest, topology, Hash(observation.AuthenticationProfile)) { ExecutableDigest = executable };
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
