#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Server.DomainServices;

internal sealed class GatewayFixtureRoutesResolver : IDomainServiceResolver
    { public Task<DomainServiceRegistration?> ResolveAsync(string tenantId, string domain, string version = "v1", CancellationToken cancellationToken = default) => Task.FromResult<DomainServiceRegistration?>(new(domain, "process", tenantId, domain, version)); }
#endif
