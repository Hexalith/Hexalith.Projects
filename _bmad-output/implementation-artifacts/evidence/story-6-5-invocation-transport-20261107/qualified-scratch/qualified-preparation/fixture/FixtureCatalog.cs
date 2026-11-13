namespace Story65.InvocationFixture;

using System.Text.Json;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal static class FixtureCatalog
{
    internal static RouteCatalogActivation Create(string path, RouteCatalogHostIdentity identity)
    {
        RouteCatalogEnvelope original = RouteCatalogCodec.Decode(File.ReadAllBytes(path));
        var declaration = original.Declarations.First(d => d.Kind == "query" && d.Execution!.TargetKind == "domain-service" && !d.Execution.RequiresDelegatedContext);
        var operation = new RouteOperationContract(declaration.RouteId, declaration.ContractVersion, "public", "agent-eligible",
            JsonSerializer.SerializeToElement(new { type = "object" }), JsonSerializer.SerializeToElement(new { type = "object" }), "",
            declaration.Kind, declaration.Domain, declaration.MessageType!);
        operation = operation with { Digest = RouteCatalogCodec.ComputeOperationContractDigest(operation) };
        var envelope = VersionedRouteCatalogBuilder.Build(new(1, "Development", [declaration], [operation],
            [new RouteCompositionBinding(declaration.RouteId, "eventstore", "query", declaration.ContractVersion, false, "*")], []));
        string digest = new('a', 64);
        var endpoint = new RouteCatalogTechnicalHttpEndpoint("/api/v1/streams/read/workload", ["POST"],
            "Hexalith.EventStore.Gateway, synthetic-scope-fixture", digest, digest, ["DaprInternal:eventstore:stream-read"], digest, digest)
        { CredentialKind = "workload" };
        var receiver = new RouteCatalogTechnicalHttpReceiver("eventstore", RouteCatalogTechnicalHttpPolicy.FenceVersion, [endpoint])
        { Invocations = [new("eventstore", "api/v1/streams/read/workload", ["POST"])] };
        var activation = new RouteCatalogActivation("synthetic-attempt", RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope),
            null, [identity], "Development-fixture", "", [new("eventstore", "api/v1/streams/read/workload", "eventstore:stream-read", "gateway")])
        { TechnicalHttpReceivers = [receiver] };
        return activation with { TrustProof = RouteCatalogActivationBinding.ComputeDigest(activation) };
    }
}
