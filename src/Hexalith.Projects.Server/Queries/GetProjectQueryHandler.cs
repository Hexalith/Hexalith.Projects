namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Routes the supported singleton authorized Project read.</summary>
public sealed class GetProjectQueryHandler(ProjectReadQueryExecutor executor) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => GetProjectQuery.QueryType;

    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => executor.GetAsync(query, cancellationToken);
}
