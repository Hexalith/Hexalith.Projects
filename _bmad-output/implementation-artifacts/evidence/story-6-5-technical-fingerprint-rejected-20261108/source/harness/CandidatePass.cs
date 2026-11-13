using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Microsoft.AspNetCore.Routing;
using Candidate = Story65.TechnicalFingerprint.Candidate;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class CandidatePass(IServiceProvider services) : IProofPass
{
    private readonly Candidate.TechnicalHttpObservationPass _pass = new(services);
    public RouteCatalogTechnicalHttpEndpoint? Describe(RouteEndpoint endpoint, bool required = true) => Candidate.TechnicalHttpActionObservation.Describe(endpoint, services, _pass, required);
    public string AssemblyIdentity(Assembly assembly) => _pass.AssemblyIdentity(assembly);
    public void Complete() => _pass.Complete();
}
