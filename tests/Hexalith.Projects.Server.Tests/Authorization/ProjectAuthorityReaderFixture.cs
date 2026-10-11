using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Projects.Server.Tests.Authorization;

internal sealed class ProjectAuthorityReaderFixture(AuthoritativeStreamReadResult result) : IAuthoritativeEventStreamReader
{
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
        => Task.FromResult(result.Stream?.Identity == identity ? result : new AuthoritativeStreamReadResult(null, "scope-mismatch"));
}
