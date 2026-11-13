using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Routing;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class ActualPass(IServiceProvider services) : IProofPass
{
    private readonly TechnicalHttpObservationPass _pass = new(services);
    public RouteCatalogTechnicalHttpEndpoint? Describe(RouteEndpoint endpoint, bool required = true) => TechnicalHttpActionObservation.Describe(endpoint, services, _pass, required);
    public string AssemblyIdentity(Assembly assembly) => _pass.AssemblyIdentity(assembly);
    public void Complete() => _pass.Complete();
}
