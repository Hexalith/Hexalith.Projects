using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Http;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Exercises the unchanged extension-guard branch, without the concrete guard timing path.</summary>
internal sealed class CustomGuard(Func<Task<RouteCatalogDispatchLease?>> acquire) : IDomainRouteCatalogGuard
{
    public Task<RouteCatalogDispatchLease?> AcquireAsync(EndpointFilterInvocationContext context) => acquire();
}
