#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.Folders.Authorization;

internal sealed class GatewayFixtureOwnerHeads : IOrganizationStreamSequenceReader
    {
        private readonly ConcurrentDictionary<(string, string), long> values = new();
        public bool Outage { get; set; }
        public void Set(string tenant, string folder, long value) => values[(tenant, folder)] = value;
        public Task<long> GetCurrentSequenceAsync(string tenantId, string organizationId, CancellationToken cancellationToken = default)
            => Outage ? Task.FromException<long>(new IOException("owner head unavailable")) : Task.FromResult(values.GetValueOrDefault((tenantId, organizationId)));
    }
#endif
