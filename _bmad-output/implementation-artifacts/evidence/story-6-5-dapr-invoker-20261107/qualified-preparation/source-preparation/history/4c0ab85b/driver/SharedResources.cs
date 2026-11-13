namespace Story65.DaprInvokerProof;

using Dapr.Client;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal static class SharedResources
{
    internal static CommittedRouteCatalogSnapshot Snapshot = null!;
    internal static RouteContractDeclaration Selected = null!, Delegated = null!;
    internal static RouteCatalogEntry Target = null!;
    internal static DaprClient Dapr = null!;
    internal static void Prepare(string catalogPath)
    {
        RouteCatalogEnvelope original = RouteCatalogCodec.Decode(File.ReadAllBytes(catalogPath));
        Selected = original.Declarations.First(d => d.Kind == "query" && d.Execution!.TargetKind == "domain-service" && !d.Execution.RequiresDelegatedContext);
        Delegated = original.Declarations.First(d => d.Kind == "query" && d.Execution!.TargetKind == "domain-service" && d.Execution.RequiresDelegatedContext);
        var envelope = RouteCatalogCodec.Seal(original with
        { Routes = original.Routes.Select(r => r.RouteId == Selected.RouteId ? r with { Method = "synthetic-committed-query" } : r).ToArray() });
        Snapshot = new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
        Target = envelope.Routes.Single(r => r.RouteId == Selected.RouteId);
        Dapr = new DaprClientBuilder().UseHttpEndpoint("http://synthetic-dapr.invalid:3500").Build();
        // Build and CreateInvokeMethodRequest only. No real Dapr or HttpClient send is ever performed.
    }
}
