#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Server.Queries;

internal sealed class GatewayFixtureNoProjectionETag : IETagService { public Task<string?> GetCurrentETagAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null); }
#endif
