using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Sample.Counter;
namespace Story65.Canonical.Harness;
/// <summary>Reads actual compiled Sample contracts and the unchanged public routing fixture, without hosts or service providers.</summary>
internal static class Datasets
{
    internal const string Root = "/home/administrator/projects/hexalith/projects";
    internal const string Fixture = Root + "/references/Hexalith.EventStore/deploy/dapr/eventstore-routing-catalog.json";
    internal static Dataset[] Load()
    {
        RouteCatalogEnvelope fixture = RouteCatalogCodec.Decode(File.ReadAllBytes(Fixture));
        var sampleDeclarations = CompiledRouteCatalogDeclarations.Read([typeof(SampleCatalog).Assembly]);
        var sampleOperations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(SampleCatalog).Assembly]);
        Check.That(sampleDeclarations.Count > 0 && sampleOperations.Count == sampleDeclarations.Count, "compiled-operation-completeness");
        return [Build("public-catalog-fixture", fixture.Declarations, fixture.Idempotency, null, fixture.Routes),
            Build("compiled-sample-owner", sampleDeclarations, [], sampleOperations, [])];
    }
    private static Dataset Build(string name, IReadOnlyList<RouteContractDeclaration> declarations,
        IReadOnlyList<IdempotencyCatalogEntry> admission, IReadOnlyList<RouteOperationContract>? operations, IReadOnlyList<RouteCatalogEntry> targets)
    {
        var observation = new RouteCatalogHostObservation("scratch-host", "scratch-app", declarations, targets, ["scratch-host"],
            [JsonSerializer.SerializeToUtf8Bytes(new { kind = "Component", metadata = new { name = "scratch-state" } }),
             JsonSerializer.SerializeToUtf8Bytes(new { spec = new { accessControl = new { defaultAction = "deny" } } })],
            "synthetic-auth-profile"u8.ToArray(), admission, [], [], new("scratch-state", "scratch-key", "http://127.0.0.1"), Operations: operations);
        return new(name, observation);
    }
    internal static object Facts(Dataset dataset) => new
    {
        dataset.Name, Declarations = dataset.Observation.Declarations.Count, AdmissionPolicies = dataset.Observation.AdmissionPolicies!.Count,
        Operations = dataset.Observation.Operations?.Count ?? 0,
        Source = dataset.Name == "compiled-sample-owner" ? "actual-compiled-SampleCatalog" : "unchanged-public-catalog-fixture",
        NoCompleteLiveHostInventoryClaim = true,
    };
    internal static string Sha(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
