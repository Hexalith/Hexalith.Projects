using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Microsoft.AspNetCore.Routing;
using Control = Story65.TechnicalFingerprint.Control;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class ControlPass(IServiceProvider services) : IProofPass
{
    private readonly Control.TechnicalHttpObservationPass _pass = new(services);
    public RouteCatalogTechnicalHttpEndpoint? Describe(RouteEndpoint endpoint, bool required = true) => Control.TechnicalHttpActionObservation.Describe(endpoint, services, _pass, required);
    public string AssemblyIdentity(Assembly assembly) => _pass.AssemblyIdentity(assembly);
    public void Complete() => _pass.Complete();
}
