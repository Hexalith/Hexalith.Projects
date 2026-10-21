namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Routes the supported advisory Attachments resolution query.</summary>
public sealed class ResolveProjectFromAttachmentsQueryHandler(ProjectAdvisoryQueryExecutor executor) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;
    /// <inheritdoc/>
    public string QueryType => ResolveProjectFromAttachmentsQuery.QueryType;
    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => executor.FromAttachmentsAsync(query, cancellationToken);
}
