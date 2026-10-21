namespace Hexalith.Projects.Server.Queries;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

/// <summary>Uses the existing signed owner gateway and preserves the original actor authority.</summary>
public sealed class DelegatedProjectAdvisoryConversationDirectory(Func<DelegatedOwnerQueryClient> client) : IProjectAdvisoryConversationDirectory
{
    /// <inheritdoc/>
    public Task<QueryResult> ReadAsync(string tenantId, string conversationId, CancellationToken cancellationToken)
        => client().InvokeAsync(new SubmitQueryRequest(tenantId, "conversations", conversationId,
            GetConversationAdvisoryMetadataQuery.QueryType, Payload: JsonSerializer.SerializeToElement(new GetConversationAdvisoryMetadataQuery())), cancellationToken);
}
