namespace Hexalith.Projects.Client;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Consumes Contracts-generated typed bindings through the existing Gateway.</summary>
/// <param name="gateway">The platform transport with caller-owned authentication.</param>
public sealed class ProjectsSupportedReadClient(IEventStoreGatewayClient gateway)
{
    /// <summary>Submits one of the seven declared reads without a transport fallback.</summary>
    /// <typeparam name="T">The exact declared result type.</typeparam>
    /// <param name="tenantId">The authenticated Tenant.</param>
    /// <param name="target">The opaque query target.</param>
    /// <param name="queryType">The existing wire discriminator.</param>
    /// <param name="payload">The typed query contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The declared result, preserving Gateway failure semantics.</returns>
    public async Task<EventStoreQueryResult<T>> QueryAsync<T>(string tenantId, string target, string queryType, object payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        object response = payload switch
        {
            ListProjectsQuery query when queryType == ListProjectsQuery.QueryType && typeof(T) == typeof(ProjectListPage)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            GetProjectQuery query when queryType == GetProjectQuery.QueryType && typeof(T) == typeof(GetProjectResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            GetConversationStartSetupQuery query when queryType == "Hexalith.Projects.Queries.GetConversationStartSetup.v1" && typeof(T) == typeof(ConversationStartSetupResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            GetProjectContextQuery query when queryType == "Hexalith.Projects.Queries.GetProjectContext.v1" && typeof(T) == typeof(ProjectContextReadResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            ExplainContextSelectionQuery query when queryType == "Hexalith.Projects.Queries.ExplainContextSelection.v1" && typeof(T) == typeof(ExplainContextSelectionResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            ResolveProjectFromConversationQuery query when queryType == ResolveProjectFromConversationQuery.QueryType && typeof(T) == typeof(ProjectAdvisoryResolutionResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            ResolveProjectFromAttachmentsQuery query when queryType == ResolveProjectFromAttachmentsQuery.QueryType && typeof(T) == typeof(ProjectAdvisoryResolutionResponse)
                => await gateway.QueryAsync(tenantId, target, query, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException("The query and result must match a declared supported read.", nameof(payload)),
        };
        return (EventStoreQueryResult<T>)response;
    }
}
