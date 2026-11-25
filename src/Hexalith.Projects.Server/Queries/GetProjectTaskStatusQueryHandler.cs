namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Handles the authenticated Durable Task status query.</summary>
[EventStoreDomain(ProjectsServerModule.DomainName)]
public sealed class GetProjectTaskStatusQueryHandler(ProjectOperatorQueryExecutor executor) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => GetProjectTaskStatusQuery.QueryType;

    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => executor.TaskStatusAsync(query, cancellationToken);
}
