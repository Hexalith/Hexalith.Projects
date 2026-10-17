namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Routes the supported authorized Project enumeration.</summary>
public sealed class ListProjectsQueryHandler(ProjectReadQueryExecutor executor) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => ListProjectsQuery.QueryType;

    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => executor.ListAsync(query, cancellationToken);
}
