namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;

/// <summary>The signed, exact-resource owner query seam for advisory Conversation metadata.</summary>
public interface IProjectAdvisoryConversationDirectory
{
    /// <summary>Reads only the isolated metadata under the current verified original caller.</summary>
    Task<QueryResult> ReadAsync(string tenantId, string conversationId, CancellationToken cancellationToken);
}
