namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Routes the supported advisory Conversation resolution query.</summary>
public sealed class ResolveProjectFromConversationQueryHandler(ProjectAdvisoryQueryExecutor executor) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;
    /// <inheritdoc/>
    public string QueryType => ResolveProjectFromConversationQuery.QueryType;
    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => executor.FromConversationAsync(query, cancellationToken);
}
