using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace Hexalith.EventStore.ServiceDefaults.Routing;
internal sealed class PlainGrantObservation(IServiceProvider services)
{
    private readonly IServiceProvider _services=services;
    internal IReadOnlyList<RouteCatalogHttpGrant> Grants(RouteCatalogTechnicalHttpEndpoint endpoint, TechnicalHttpObservationPass pass)
    {
        string? json = _services!.GetRequiredService<IConfiguration>()["EventStore:RouteCatalog:DevelopmentBootstrap:DeclaredInvocations"];
        var invocations = pass.DeclaredInvocations(json);
        return invocations.Where(invocation => !invocation.Method.Contains('*', StringComparison.Ordinal)
            && invocation.HttpMethods.Any(verb => endpoint.Methods.Contains(verb, StringComparer.Ordinal)
                && PlainRouteTemplate.Matches(endpoint.Pattern, invocation.Method.Trim('/'), verb)))
            .Select(invocation => new RouteCatalogHttpGrant(invocation.Method, string.Join(' ', invocation.HttpMethods.Intersect(endpoint.Methods, StringComparer.Ordinal).Order(StringComparer.Ordinal))))
            .Distinct().OrderBy(grant => grant.Method, StringComparer.Ordinal).ThenBy(grant => grant.Verbs, StringComparer.Ordinal).ToArray();
    }

}
