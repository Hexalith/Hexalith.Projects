using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Microsoft.AspNetCore.Routing;
namespace Story65.TechnicalFingerprint.Harness;
internal interface IProofPass
{
    RouteCatalogTechnicalHttpEndpoint? Describe(RouteEndpoint endpoint, bool required = true);
    string AssemblyIdentity(Assembly assembly);
    void Complete();
}
