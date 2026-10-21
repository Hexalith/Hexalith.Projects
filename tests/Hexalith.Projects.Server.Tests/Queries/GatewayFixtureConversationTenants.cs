#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Client.Projections;

/// <summary>Controlled health fixture over real SDK storage; the production Conversation access evaluator is unchanged.</summary>
internal sealed class GatewayFixtureConversationTenants(IReadModelStore store) : ITenantProjectionStore, IConversationTenantProjectionSignal
{
    public static string Key(string tenant) => $"story64:conversation-tenant:{tenant}";
    public async Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        => (await store.GetAsync<TenantLocalState>("statestore", Key(tenantId), cancellationToken).ConfigureAwait(false)).Value;
    public async Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
        => _ = await ReadModelWritePolicy.UpdateAsync<TenantLocalState>(store, "statestore", Key(state.TenantId), _ => state, cancellationToken: cancellationToken).ConfigureAwait(false);
    public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(string tenantId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new ConversationTenantProjectionHealth(Version: 1, Watermark: tenantId + ":1"));
}
#endif
