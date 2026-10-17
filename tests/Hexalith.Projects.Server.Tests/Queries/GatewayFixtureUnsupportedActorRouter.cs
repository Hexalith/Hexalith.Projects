#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Server.Queries;

internal sealed class GatewayFixtureUnsupportedActorRouter : IQueryRouter
    { public Task<QueryRouterResult> RouteQueryAsync(Hexalith.EventStore.Server.Pipeline.Queries.SubmitQuery query, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Catalog failed to route a registered handler."); }
#endif
